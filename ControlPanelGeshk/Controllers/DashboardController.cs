using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize] // el panel es interno
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// KPIs del dashboard. Ej: /dashboard?from=2025-01-01&to=2025-12-31
    /// </summary>
    /// // DashboardController.cs
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        // 1) Clientes (excluir eliminados)
        var nClients = await _db.Clients.CountAsync(c => !c.IsDeleted, ct);

        // 2) Proyectos activos (excluir eliminados)
        var activeOrders = await _db.Projects
            .CountAsync(p => !p.IsDeleted && (p.Status == "Activo" || p.Status == "En Proceso"), ct);

        // 3) Servicios entregados en rango (excluir eliminados)
        var servicesDelivered = await _db.Projects
            .CountAsync(p => !p.IsDeleted &&
                             p.DeliveredAt != null &&
                             p.DeliveredAt >= fromDt &&
                             p.DeliveredAt <= toDt, ct);

        // 4) Ingresos y Egresos (excluir eliminados)
        var qTx = _db.Transactions.AsNoTracking()
            .Where(t => !t.IsDeleted && t.Date >= from && t.Date <= to);

        var revenue = await qTx.Where(t => t.Type == "Ingreso")
                               .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var expense = await qTx.Where(t => t.Type == "Egreso")
                               .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var net = revenue - expense;

        // 5) Últimos servicios (excluir eliminados)
        var lastServices = await _db.Projects
            .Where(p => !p.IsDeleted && p.DeliveredAt != null)
            .OrderByDescending(p => p.DeliveredAt)
            .Select(p => p.Id)
            .Take(5)
            .ToListAsync(ct);

        return Ok(new DashboardSummaryDto(
            nClients,
            activeOrders,
            servicesDelivered,
            revenue,
            expense,
            net,
            lastServices
        ));
    }

}
