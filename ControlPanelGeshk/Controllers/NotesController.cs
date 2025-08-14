using System.Security.Claims;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public NotesController(ApplicationDbContext db) => _db = db;

    // GET /notes?scopeType=Proyecto&scopeId=...&pinned=&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<NoteDto>>> List(
        [FromQuery] string scopeType,
        [FromQuery] Guid scopeId,
        [FromQuery] bool? pinned,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var q = _db.Notes.AsNoTracking()
            .Where(n => !n.IsDeleted && n.ScopeType == scopeType && n.ScopeId == scopeId);

        if (pinned.HasValue)
            q = q.Where(n => n.IsPinned == pinned.Value);

        // pila: fijadas primero; luego más recientes
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteDto(
                n.Id,
                n.ScopeType,
                n.ScopeId,
                n.Content,
                n.IsPinned,
                _db.Users.Where(u => u.Id == n.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                n.CreatedAt,
                n.EditedAt,
                n.EditedBy != null
                    ? _db.Users.Where(u => u.Id == n.EditedBy).Select(u => u.Name).FirstOrDefault()
                    : null
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<NoteDto>(items, page, pageSize, total));
    }

    // TOP de pila (proyecto): /projects/{projectId}/notes/top?count=3
    [HttpGet("/projects/{projectId:guid}/notes/top")]
    public async Task<ActionResult<NoteDto[]>> TopForProject(Guid projectId, [FromQuery] int count = 3, CancellationToken ct = default)
    {
        count = count is < 1 or > 10 ? 3 : count;

        var top = await _db.Notes.AsNoTracking()
            .Where(n => !n.IsDeleted && n.ScopeType == "Proyecto" && n.ScopeId == projectId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Take(count)
            .Select(n => new NoteDto(
                n.Id,
                n.ScopeType,
                n.ScopeId,
                n.Content,
                n.IsPinned,
                _db.Users.Where(u => u.Id == n.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                n.CreatedAt,
                n.EditedAt,
                n.EditedBy != null
                    ? _db.Users.Where(u => u.Id == n.EditedBy).Select(u => u.Name).FirstOrDefault()
                    : null
            ))
            .ToArrayAsync(ct);

        return Ok(top);
    }

    // TOP de pila (cliente): /clients/{clientId}/notes/top?count=3
    [HttpGet("/clients/{clientId:guid}/notes/top")]
    public async Task<ActionResult<NoteDto[]>> TopForClient(Guid clientId, [FromQuery] int count = 3, CancellationToken ct = default)
    {
        count = count is < 1 or > 10 ? 3 : count;

        var top = await _db.Notes.AsNoTracking()
            .Where(n => !n.IsDeleted && n.ScopeType == "Cliente" && n.ScopeId == clientId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Take(count)
            .Select(n => new NoteDto(
                n.Id,
                n.ScopeType,
                n.ScopeId,
                n.Content,
                n.IsPinned,
                _db.Users.Where(u => u.Id == n.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                n.CreatedAt,
                n.EditedAt,
                n.EditedBy != null
                    ? _db.Users.Where(u => u.Id == n.EditedBy).Select(u => u.Name).FirstOrDefault()
                    : null
            ))
            .ToArrayAsync(ct);

        return Ok(top);
    }

    // POST /notes
    [HttpPost]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Create([FromBody] NoteCreateDto dto, CancellationToken ct)
    {
        if (dto.ScopeType != "Cliente" && dto.ScopeType != "Proyecto")
            return BadRequest(new { message = "ScopeType inválido. Usa Cliente o Proyecto." });

        // Validar que el scope exista
        var exists = dto.ScopeType == "Cliente"
            ? await _db.Clients.AnyAsync(c => c.Id == dto.ScopeId && !c.IsDeleted, ct)
            : await _db.Projects.AnyAsync(p => p.Id == dto.ScopeId && !p.IsDeleted, ct);

        if (!exists) return BadRequest(new { message = "ScopeId inválido." });

        var userId = GetUserId();
        var note = new Entities.Note
        {
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Content = dto.Content.Trim(),
            IsPinned = dto.IsPinned,
            CreatedBy = userId
        };
        _db.Notes.Add(note);

        // (Opcional) registrar actividad mínima
        _db.Activities.Add(new Entities.Activity
        {
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Type = "NoteAdded",
            ActorUserId = userId,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = note.Id }, new { note.Id });
    }

    // GET /notes/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> GetById(Guid id, CancellationToken ct)
    {
        var n = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (n == null) return NotFound();

        var dto = new NoteDto(
            n.Id,
                n.ScopeType,
                n.ScopeId,
                n.Content,
                n.IsPinned,
                _db.Users.Where(u => u.Id == n.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                n.CreatedAt,
                n.EditedAt,
                n.EditedBy != null
                    ? _db.Users.Where(u => u.Id == n.EditedBy).Select(u => u.Name).FirstOrDefault()
                    : null
        );
        return Ok(dto);
    }

    // PUT /notes/{id}  (editar contenido + pin)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Update(Guid id, [FromBody] NoteUpdateDto dto, CancellationToken ct)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (n == null) return NotFound();

        n.Content = dto.Content.Trim();
        n.IsPinned = dto.IsPinned;
        n.EditedAt = DateTimeOffset.UtcNow;
        n.EditedBy = GetUserId();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PATCH /notes/{id}/pin  (solo fijar/quitar pin)
    [HttpPatch("{id:guid}/pin")]
    [Authorize(Roles = "Admin,Director,Operativo")]
    public async Task<ActionResult> Pin(Guid id, [FromBody] NotePinUpdateDto dto, CancellationToken ct)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (n == null) return NotFound();

        n.IsPinned = dto.IsPinned;
        n.EditedAt = DateTimeOffset.UtcNow;
        n.EditedBy = GetUserId();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /notes/{id}  (soft-delete; desalienta borrar)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (n == null) return NotFound();

        n.IsDeleted = true;
        n.EditedAt = DateTimeOffset.UtcNow;
        n.EditedBy = GetUserId();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // -------------- helpers --------------
    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
