using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SearchController(ApplicationDbContext db) => _db = db;

    // GET /search?q=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchItemDto>>> Search([FromQuery] string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<SearchItemDto>());

        var s = q.Trim().ToLower();

        // Clients
        var clients = await _db.Clients.AsNoTracking()
            .Where(c => !c.IsDeleted &&
                        (c.BusinessName.ToLower().Contains(s) ||
                         c.ClientName.ToLower().Contains(s)))
            .OrderBy(c => c.BusinessName)
            .Take(10)
            .Select(c => new SearchItemDto(
                "Client",
                c.Id,
                c.BusinessName,
                c.ClientName,
                null
            ))
            .ToListAsync(ct);

        // Projects
        var projects = await _db.Projects.AsNoTracking()
            .Include(p => p.Client)
            .Where(p => !p.IsDeleted &&
                        (p.Name.ToLower().Contains(s) ||
                         (p.SiteUrl != null && p.SiteUrl.ToLower().Contains(s)) ||
                         (p.Subdomain != null && p.Subdomain.ToLower().Contains(s)) ||
                         p.Client.BusinessName.ToLower().Contains(s)))
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new SearchItemDto(
                "Project",
                p.Id,
                p.Name,
                p.Client.BusinessName,
                p.SiteUrl ?? (p.Subdomain ?? null)
            ))
            .ToListAsync(ct);

        // Próximas reuniones que hagan match
        var nowUtc = DateTimeOffset.UtcNow;
        var meetings = await _db.Meetings.AsNoTracking()
            .Include(m => m.Project)
            .ThenInclude(p => p.Client)
            .Where(m => m.ScheduledAt >= nowUtc &&
                        (m.Notes != null && m.Notes.ToLower().Contains(s) ||
                         (m.Project != null && m.Project.Name.ToLower().Contains(s)) ||
                         (m.Project != null && m.Project.Client.BusinessName.ToLower().Contains(s))))
            .OrderBy(m => m.ScheduledAt)
            .Take(10)
            .Select(m => new SearchItemDto(
                "Meeting",
                m.Id,
                m.Project != null ? $"Reunión · {m.Project.Name}" : "Reunión",
                m.ScheduledAt.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                m.Location
            ))
            .ToListAsync(ct);

        var result = new List<SearchItemDto>();
        result.AddRange(clients);
        result.AddRange(projects);
        result.AddRange(meetings);

        return Ok(result);
    }
}
