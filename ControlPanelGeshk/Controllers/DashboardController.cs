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
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        // Bordes de fecha
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        // 1) Clientes
        var nClients = await _db.Clients.CountAsync(ct);

        // 2) Proyectos activos ("Activo" o "En Proceso")
        var activeOrders = await _db.Projects
            .CountAsync(p => p.Status == "Activo" || p.Status == "En Proceso", ct);

        // 3) Servicios entregados dentro del rango
        var servicesDelivered = await _db.Projects
            .CountAsync(p => p.DeliveredAt != null &&
                             p.DeliveredAt >= fromDt &&
                             p.DeliveredAt <= toDt, ct);

        // 4) Revenue (ingresos) dentro del rango
        var revenue = await _db.Transactions
            .Where(t => t.Type == "Ingreso" &&
                        t.Date >= from &&
                        t.Date <= to)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        // 5) Últimos servicios entregados (IDs)
        var lastServicesInRange = await _db.Projects
            .Where(p => p.DeliveredAt != null &&
                        p.DeliveredAt >= fromDt &&
                        p.DeliveredAt <= toDt)
            .OrderByDescending(p => p.DeliveredAt)
            .Select(p => p.Id)
            .Take(5)
            .ToListAsync(ct);

        List<Guid> lastServices = lastServicesInRange;
        if (lastServices.Count == 0)
        {
            lastServices = await _db.Projects
                .Where(p => p.DeliveredAt != null)
                .OrderByDescending(p => p.DeliveredAt)
                .Select(p => p.Id)
                .Take(5)
                .ToListAsync(ct);
        }

        return Ok(new DashboardSummaryDto(
            nClients,
            activeOrders,
            servicesDelivered,
            revenue,
            lastServices
        ));
    }
}
