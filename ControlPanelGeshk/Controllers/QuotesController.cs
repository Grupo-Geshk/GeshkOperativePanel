using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public QuotesController(ApplicationDbContext db) => _db = db;

    // ---------------- helpers de fecha ----------------
    // Entity -> DTO
    private static DateTimeOffset? ToDateTimeOffset(DateOnly? d) =>
        d.HasValue
            ? new DateTimeOffset(d.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : (DateTimeOffset?)null;

    // DTO -> Entity
    private static DateOnly? ToDateOnly(DateTimeOffset? d) =>
        d.HasValue
            ? DateOnly.FromDateTime(d.Value.UtcDateTime)
            : (DateOnly?)null;

    private static string NormalizeRole(string r)
    {
        var s = (r ?? "").Trim();
        return s.Equals("Revenue", StringComparison.OrdinalIgnoreCase) ? "Revenue" : "Cost";
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    // ---------------- LISTA POR PROYECTO ----------------
    // GET /projects/{id}/quotes
    [HttpGet("/projects/{id:guid}/quotes")]
    public async Task<ActionResult<IEnumerable<QuoteListItemDto>>> ListByProject(Guid id, CancellationToken ct)
    {
        var exists = await _db.Projects.AsNoTracking().AnyAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (!exists) return NotFound(new { message = "Proyecto no encontrado." });

        var list = await _db.Quotes.AsNoTracking()
            .Where(q => q.ProjectId == id)
            .OrderByDescending(q => q.Version)
            .Select(q => new QuoteListItemDto(
                q.Id,
                q.ProjectId,
                q.Code,
                q.Version,
                q.Status,
                q.Currency,
                ToDateTimeOffset(q.ValidUntil), // <- Entity(DateOnly?) -> DTO(DateTimeOffset?)
                q.OneOffPrice,
                q.MonthlyFee,
                q.CreatedAt
            ))
            .ToListAsync(ct);

        return Ok(list);
    }

    // ---------------- CREAR ----------------
    // POST /projects/{id}/quotes
    [HttpPost("/projects/{id:guid}/quotes")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Create(Guid id, [FromBody] QuoteCreateDto dto, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (project == null) return BadRequest(new { message = "Proyecto inválido." });

        // Nuevo versionado: último + 1
        var nextVersion = (await _db.Quotes.AsNoTracking()
            .Where(q => q.ProjectId == id)
            .Select(q => (int?)q.Version).MaxAsync(ct)) ?? 0;
        nextVersion += 1;

        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? $"Q-{DateTime.UtcNow:yyyyMMdd}-{nextVersion:D2}"
            : dto.Code!.Trim();

        var quote = new Entities.Quote
        {
            ProjectId = id,
            Code = code,
            Version = nextVersion,
            Status = "Draft",
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim(),
            ValidUntil = ToDateOnly(dto.ValidUntil), // <- DTO(DateTimeOffset?) -> Entity(DateOnly?)
            Terms = dto.Terms,
            OneOffPrice = dto.OneOffPrice,
            MonthlyFee = dto.MonthlyFee,
            CreatedBy = GetUserId(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Items
        if (dto.Items is { Count: > 0 })
        {
            foreach (var it in dto.Items)
            {
                quote.Items.Add(new Entities.QuoteItem
                {
                    Concept = it.Concept.Trim(),
                    Role = NormalizeRole(it.Role),
                    Category = it.Category.Trim(),
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice
                });
            }
        }

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { quoteId = quote.Id }, new { quote.Id });
    }

    // ---------------- OBTENER ----------------
    // GET /quotes/{quoteId}
    [HttpGet("/quotes/{quoteId:guid}")]
    public async Task<ActionResult<QuoteDetailDto>> GetById(Guid quoteId, CancellationToken ct)
    {
        var q = await _db.Quotes
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteId, ct);

        if (q == null) return NotFound();

        var items = q.Items
            .OrderBy(i => i.Role).ThenBy(i => i.Category).ThenBy(i => i.Concept)
            .Select(i => new QuoteItemDto(
                i.Id, i.Concept, i.Role, i.Category, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice
            ))
            .ToArray();

        var itemsRevenue = items.Where(i => i.Role == "Revenue").Sum(i => i.Total);
        var itemsCost = items.Where(i => i.Role == "Cost").Sum(i => i.Total);

        var dto = new QuoteDetailDto(
            q.Id,
            q.ProjectId,
            q.Code,
            q.Version,
            q.Status,
            q.Currency,
            ToDateTimeOffset(q.ValidUntil), // <- Entity -> DTO
            q.Terms,
            q.OneOffPrice,
            q.MonthlyFee,
            items,
            itemsRevenue,
            itemsCost,
            q.CreatedAt,
            q.CreatedBy
        );

        return Ok(dto);
    }

    // ---------------- EDITAR ----------------
    // PUT /quotes/{quoteId}
    [HttpPut("/quotes/{quoteId:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Update(Guid quoteId, [FromBody] QuoteUpdateDto dto, CancellationToken ct)
    {
        var q = await _db.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quoteId, ct);
        if (q == null) return NotFound();

        if (q.Status == "Approved")
            return BadRequest(new { message = "No se puede editar una cotización aprobada." });

        if (!string.IsNullOrWhiteSpace(dto.Code)) q.Code = dto.Code!.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Currency)) q.Currency = dto.Currency!.Trim();
        q.ValidUntil = ToDateOnly(dto.ValidUntil); // <- DTO -> Entity
        q.Terms = dto.Terms;
        if (dto.OneOffPrice.HasValue) q.OneOffPrice = dto.OneOffPrice.Value;
        if (dto.MonthlyFee.HasValue) q.MonthlyFee = dto.MonthlyFee.Value;

        // Reemplaza items si vienen
        if (dto.Items != null)
        {
            q.Items.Clear();
            foreach (var it in dto.Items)
            {
                q.Items.Add(new Entities.QuoteItem
                {
                    Concept = it.Concept.Trim(),
                    Role = NormalizeRole(it.Role),
                    Category = it.Category.Trim(),
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice
                });
            }
        }

        q.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------------- APROBAR ----------------
    // PUT /quotes/{quoteId}/approve
    [HttpPut("/quotes/{quoteId:guid}/approve")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Approve(Guid quoteId, [FromBody] QuoteApproveDto body, CancellationToken ct)
    {
        var q = await _db.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId, ct);
        if (q == null) return NotFound();

        if (q.Status == "Approved") return NoContent(); // idempotente

        q.Status = "Approved";
        q.UpdatedAt = DateTimeOffset.UtcNow;

        // Registrar actividad con jsonb (JsonDocument)
        var payloadObj = new { quoteId, q.Code, note = body?.Note };
        var payloadJson = JsonSerializer.Serialize(payloadObj);
        var payloadDoc = JsonDocument.Parse(payloadJson);

        _db.Activities.Add(new Entities.Activity
        {
            ScopeType = "Proyecto",
            ScopeId = q.ProjectId,
            Type = "QuoteApproved",
            ActorUserId = GetUserId(),
            OccurredAt = DateTimeOffset.UtcNow,
            Payload = payloadDoc
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
