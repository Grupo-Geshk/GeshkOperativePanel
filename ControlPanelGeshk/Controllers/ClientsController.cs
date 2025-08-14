using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ClientsController(ApplicationDbContext db) => _db = db;

    // GET /clients?search=&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ClientListItemDto>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var q = _db.Clients.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c =>
                c.BusinessName.ToLower().Contains(s) ||
                c.ClientName.ToLower().Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(s)) ||
                (c.Location != null && c.Location.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(c => c.BusinessName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientListItemDto(
                c.Id, c.BusinessName, c.ClientName, c.Phone, c.Email, c.Location,
                _db.Projects.Count(p => p.ClientId == c.Id && !p.IsDeleted)
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<ClientListItemDto>(items, page, pageSize, total));
    }

    // GET /clients/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var c = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c == null) return NotFound();

        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.ClientId == id && !p.IsDeleted)
            .OrderByDescending(p => p.DeliveredAt ?? p.CreatedAt)
            .Select(p => new ProjectMiniDto(p.Id, p.Name, p.Status))
            .ToArrayAsync(ct);

        var openIssues = await _db.Issues.CountAsync(i => i.Project.ClientId == id && i.Status != "Resolved", ct);
        var lastDelivery = await _db.Projects
            .Where(p => p.ClientId == id && p.DeliveredAt != null)
            .MaxAsync(p => (DateTimeOffset?)p.DeliveredAt, ct);

        var lastPayment = await _db.Transactions
            .Where(t => t.ClientId == id && t.Type == "Ingreso")
            .OrderByDescending(t => t.Date)
            .Select(t => (DateOnly?)t.Date)
            .FirstOrDefaultAsync(ct);

        var dto = new ClientDetailDto(
            c.Id, c.BusinessName, c.ClientName, c.Phone, c.Email, c.Location, c.NotesBrief,
            projects.Length, openIssues, lastDelivery, lastPayment, projects
        );

        return Ok(dto);
    }

    // POST /clients
    [HttpPost]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Create([FromBody] ClientCreateDto dto, CancellationToken ct)
    {
        var entity = new Entities.Client
        {
            BusinessName = dto.BusinessName.Trim(),
            ClientName = dto.ClientName.Trim(),
            Phone = dto.Phone,
            Email = dto.Email?.Trim().ToLower(),
            Location = dto.Location,
            NotesBrief = dto.NotesBrief
        };
        _db.Clients.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { entity.Id });
    }

    // PUT /clients/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Update(Guid id, [FromBody] ClientUpdateDto dto, CancellationToken ct)
    {
        var c = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c == null) return NotFound();

        c.BusinessName = dto.BusinessName.Trim();
        c.ClientName = dto.ClientName.Trim();
        c.Phone = dto.Phone;
        c.Email = dto.Email?.Trim().ToLower();
        c.Location = dto.Location;
        c.NotesBrief = dto.NotesBrief;
        c.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /clients/{id} (soft)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var c = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c == null) return NotFound();
        c.IsDeleted = true;
        c.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
