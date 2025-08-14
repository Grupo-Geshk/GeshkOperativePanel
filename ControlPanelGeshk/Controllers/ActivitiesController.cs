using System.Text.Json;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("activities")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ActivitiesController(ApplicationDbContext db) => _db = db;

    // GET /activities?scopeType=&scopeId=&type=&from=&to=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> List(
    [FromQuery] string? scopeType,
    [FromQuery] Guid? scopeId,
    [FromQuery] string? type,
    [FromQuery] DateOnly? from,
    [FromQuery] DateOnly? to,
    CancellationToken ct = default)
    {
        var q = _db.Activities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(scopeType))
            q = q.Where(a => a.ScopeType == scopeType);

        if (scopeId.HasValue)
            q = q.Where(a => a.ScopeId == scopeId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(a => a.Type == type);

        if (from.HasValue)
        {
            var f = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(a => a.OccurredAt >= f);
        }
        if (to.HasValue)
        {
            var tExclusive = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(a => a.OccurredAt < tExclusive);
        }

        // 1) Proyecta solo datos "traducibles a SQL"
        var rows = await q
            .OrderByDescending(a => a.OccurredAt)
            .Take(500)
            .Select(a => new
            {
                a.Id,
                a.ScopeType,
                a.ScopeId,
                a.Type,
                a.OccurredAt,
                ActorName = _db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.Name).FirstOrDefault(),
                a.Payload
            })
            .ToListAsync(ct);

        // 2) Serializa Payload en memoria (fuera del árbol de expresión)
        var items = rows.Select(r => new ActivityDto(
            r.Id,
            r.ScopeType,
            r.ScopeId,
            r.Type,
            r.OccurredAt,
            r.ActorName ?? "—",
            r.Payload != null ? System.Text.Json.JsonSerializer.Serialize(r.Payload) : null
        )).ToList();

        return Ok(items);
    }
}
