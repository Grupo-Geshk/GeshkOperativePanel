using System.Security.Claims;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Authorize]
public class EconomicsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EconomicsController(ApplicationDbContext db) { _db = db; }

    // 1) GET /projects/{id}/economics/plan  (lista escenarios del proyecto)
    [HttpGet("/projects/{id:guid}/economics/plan")]
    public async Task<ActionResult<IEnumerable<EconomicsPlanDto>>> ListPlans(Guid id, CancellationToken ct)
    {
        // Verifica que el proyecto exista
        var exists = await _db.Projects.AsNoTracking().AnyAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (!exists) return NotFound(new { message = "Proyecto no encontrado." });

        var plans = await _db.EconomicsPlans.AsNoTracking()
        .Where(e => e.ProjectId == id)
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => new EconomicsPlanDto(
            e.Id,
            e.ProjectId,
            e.ScenarioName,
            e.PlannedOneOffRevenue,
            e.PlannedMonthlyRevenue,
            e.PlannedInternalHours,
            e.HourlyRate,
            e.PlannedInfraMonthly,
            e.PlannedThirdPartyMonthly,
            e.PlannedOneOffCosts,
            (e.PlannedInternalHours * e.HourlyRate / 160m) + e.PlannedInfraMonthly + e.PlannedThirdPartyMonthly, // PlannedMonthlyCost
            (e.PlannedOneOffRevenue - e.PlannedOneOffCosts) +
            (e.PlannedMonthlyRevenue - ((e.PlannedInternalHours * e.HourlyRate / 160m) + e.PlannedInfraMonthly + e.PlannedThirdPartyMonthly)), // PlannedMargin
            e.Notes,
            e.CreatedAt,
            e.CreatedBy
        ))
        .ToListAsync(ct);


        return Ok(plans);
    }

    // 2) POST /projects/{id}/economics/plan (crea un escenario)
    [HttpPost("/projects/{id:guid}/economics/plan")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> CreatePlan(Guid id, [FromBody] EconomicsPlanCreateDto dto, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (project == null) return BadRequest(new { message = "Proyecto inválido." });

        var plan = new Entities.EconomicsPlan
        {
            ProjectId = id,
            ScenarioName = dto.ScenarioName.Trim(),
            PlannedOneOffRevenue = dto.PlannedOneOffRevenue,
            PlannedMonthlyRevenue = dto.PlannedMonthlyRevenue,
            PlannedInternalHours = dto.PlannedInternalHours,
            HourlyRate = dto.HourlyRate,
            PlannedInfraMonthly = dto.PlannedInfraMonthly,
            PlannedThirdPartyMonthly = dto.PlannedThirdPartyMonthly,
            PlannedOneOffCosts = dto.PlannedOneOffCosts,
            Notes = dto.Notes,
            CreatedBy = GetUserId(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.EconomicsPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListPlans), new { id }, new { plan.Id });
    }

    // 3) GET /finance/margin/project/{id}?from=&to=
    // Compara plan (último creado) vs real (transactions) en rango
    [HttpGet("/finance/margin/project/{id:guid}")]
    public async Task<ActionResult<MarginCompareDto>> CompareMargin(Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (project == null) return NotFound(new { message = "Proyecto inválido." });

        var plan = await _db.EconomicsPlans.AsNoTracking()
            .Where(e => e.ProjectId == id)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (plan == null) return NotFound(new { message = "No hay plan económico para este proyecto." });

        // Real a partir de transactions en el rango
        var q = _db.Transactions.AsNoTracking()
            .Where(t => !t.IsDeleted && t.ProjectId == id && t.Date >= from && t.Date <= to);

        var realIncome = await q.Where(t => t.Type == "Ingreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var realCost = await q.Where(t => t.Type == "Egreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        // Heurística mensual: proyección de costos de mano de obra por horas*tarifa/160
        var plannedMonthlyCost = (plan.PlannedInternalHours * plan.HourlyRate / 160m)
                                 + plan.PlannedInfraMonthly
                                 + plan.PlannedThirdPartyMonthly;

        var plannedDto = new EconomicsPlanDto(
            plan.Id, plan.ProjectId, plan.ScenarioName,
            plan.PlannedOneOffRevenue, plan.PlannedMonthlyRevenue, plan.PlannedInternalHours, plan.HourlyRate,
            plan.PlannedInfraMonthly, plan.PlannedThirdPartyMonthly, plan.PlannedOneOffCosts,
            PlannedMonthlyCost: plannedMonthlyCost,
            PlannedMargin: (plan.PlannedOneOffRevenue - plan.PlannedOneOffCosts) + (plan.PlannedMonthlyRevenue - plannedMonthlyCost),
            plan.Notes, plan.CreatedAt, plan.CreatedBy
        );

        // Para el “real” separamos ingresos/egresos en bruto; si quieres, luego afinamos por categorías.
        var actual = new MarginBreakdownDto(
            OneOffRevenue: realIncome,                    // ingresos en rango
            MonthlyRevenue: 0m,                           // opcional separar si filtras por categoría "Mensualidad"
            MonthlyCost: realCost,                        // todos los egresos del rango
            OneOffCost: 0m                                // si registras costos one-off explícitos por categoría
        );

        var variance = new MarginBreakdownDto(
            actual.OneOffRevenue - (plannedDto.PlannedOneOffRevenue + plannedDto.PlannedMonthlyRevenue),
            actual.MonthlyRevenue - plannedDto.PlannedMonthlyRevenue,
            actual.MonthlyCost - plannedDto.PlannedMonthlyCost,
            actual.OneOffCost - plannedDto.PlannedOneOffCosts
        );

        return Ok(new MarginCompareDto(plannedDto, actual, variance));
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
