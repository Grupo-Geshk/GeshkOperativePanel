using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AuditController(ApplicationDbContext db) => _db = db;

    // GET /audit?entity=&actor=&from=&to=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> List(
        [FromQuery] string? entity,
        [FromQuery] Guid? actor,            // userId
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entity))
            q = q.Where(a => a.Entity == entity);

        if (actor.HasValue)
            q = q.Where(a => a.ActorUserId == actor.Value);

        if (from.HasValue)
        {
            var f = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(a => a.At >= f);
        }
        if (to.HasValue)
        {
            var tExclusive = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(a => a.At < tExclusive);
        }

        var items = await q
    .OrderByDescending(a => a.At)
    .Take(500)
    .Select(a => new AuditLogDto(
        a.Id,
        a.Action,
        a.Entity,
        a.EntityId.ToString(), // <-- conversión explícita a string
        _db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.Name).FirstOrDefault() ?? "—",
        a.At
    ))
    .ToListAsync(ct);

        return Ok(items);
    }
}
