using System.Security.Claims;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Authorize]
[Route("issues")]
public class IssuesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public IssuesController(ApplicationDbContext db) => _db = db;

    // LISTAR por proyecto: /projects/{projectId}/issues?status=&severity=&q=&page=&pageSize=
    [HttpGet("/projects/{projectId:guid}/issues")]
    public async Task<ActionResult<PagedResult<IssueDto>>> ListByProject(
        Guid projectId,
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = _db.Issues.AsNoTracking().Where(i => i.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = NormalizeStatus(status);
            query = query.Where(i => NormalizeStatus(i.Status) == s);
        }
        if (!string.IsNullOrWhiteSpace(severity))
        {
            var sev = NormalizeSeverity(severity);
            query = query.Where(i => NormalizeSeverity(i.Severity) == sev);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(s) ||
                (i.Description != null && i.Description.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new IssueDto(
                i.Id,
                i.ProjectId,
                i.Title,
                i.Description,
                i.Severity,
                i.Status,
                _db.Users.Where(u => u.Id == i.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                i.CreatedAt,
                i.ResolvedAt
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<IssueDto>(items, page, pageSize, total));
    }

    // GET /issues/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IssueDto>> Get(Guid id, CancellationToken ct)
    {
        var i = await _db.Issues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();

        var dto = new IssueDto(
             i.Id,
             i.ProjectId,
             i.Title,
             i.Description,
             i.Severity,
             i.Status,
             await _db.Users.Where(u => u.Id == i.CreatedBy).Select(u => u.Name).FirstOrDefaultAsync(ct) ?? "—",
             i.CreatedAt,
             i.ResolvedAt
         );

        return Ok(dto);
    }

    // POST /projects/{projectId}/issues
    [HttpPost("/projects/{projectId:guid}/issues")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Create(Guid projectId, [FromBody] IssueCreateDto dto, CancellationToken ct)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted, ct);
        if (!projectExists) return BadRequest(new { message = "Proyecto inválido" });

        var sev = NormalizeSeverity(dto.Severity);
        if (!AllowedSeverities.Contains(sev))
            return BadRequest(new { message = "Severity inválido. Usa Low, Med, High." });

        var userId = GetUserId();
        var entity = new Entities.Issue
        {
            ProjectId = projectId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Severity = sev,
            Status = "Open",
            CreatedBy = userId
        };
        _db.Issues.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { entity.Id });
    }

    // PUT /issues/{id}  (editar título/descr/severity y opcionalmente status)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Update(Guid id, [FromBody] IssueUpdateDto dto, CancellationToken ct)
    {
        var i = await _db.Issues.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();

        i.Title = dto.Title.Trim();
        i.Description = dto.Description;

        if (!string.IsNullOrWhiteSpace(dto.Severity))
        {
            var sev = NormalizeSeverity(dto.Severity);
            if (!AllowedSeverities.Contains(sev))
                return BadRequest(new { message = "Severity inválido. Usa Low, Med, High." });
            i.Severity = sev;
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var st = NormalizeStatus(dto.Status);
            if (!AllowedStatuses.Contains(st))
                return BadRequest(new { message = "Status inválido. Usa Open, In Progress, Resolved, Won't Fix." });

            i.Status = st;
            i.ResolvedAt = st == "Resolved" ? DateTimeOffset.UtcNow : null;
        }

        i.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PATCH /issues/{id}/status { status: "..." }
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> ChangeStatus(Guid id, [FromBody] IssueStatusUpdateDto dto, CancellationToken ct)
    {
        var i = await _db.Issues.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();

        var st = NormalizeStatus(dto.Status);
        if (!AllowedStatuses.Contains(st))
            return BadRequest(new { message = "Status inválido. Usa Open, In Progress, Resolved, Won't Fix." });

        i.Status = st;
        i.ResolvedAt = st == "Resolved" ? DateTimeOffset.UtcNow : null;
        i.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PATCH /issues/{id}/resolve (atajo)
    [HttpPatch("{id:guid}/resolve")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Resolve(Guid id, CancellationToken ct)
    {
        var i = await _db.Issues.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();

        i.Status = "Resolved";
        i.ResolvedAt = DateTimeOffset.UtcNow;
        i.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /issues/{id} (hard delete) — si prefieres soft, añade IsDeleted al entity
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var i = await _db.Issues.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();
        _db.Issues.Remove(i);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------------- helpers ----------------
    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private static readonly HashSet<string> AllowedSeverities = new(StringComparer.Ordinal)
    { "Low", "Med", "High" };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    { "Open", "In Progress", "Resolved", "Won't Fix" };

    private static string NormalizeSeverity(string s)
        => string.IsNullOrWhiteSpace(s) ? "" : char.ToUpperInvariant(s.Trim()[0]) + s.Trim().ToLowerInvariant()[1..];

    private static string NormalizeStatus(string s)
        => (s ?? "").Trim().Replace("’", "'"); // acepta apóstrofo curvo
}
