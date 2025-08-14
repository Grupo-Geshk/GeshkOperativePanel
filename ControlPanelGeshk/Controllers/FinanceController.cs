using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _cfg;
    public FinanceController(ApplicationDbContext db, IConfiguration cfg)
    {
        _db = db; _cfg = cfg;
    }

    // ------------------- SUMMARY -------------------
    // GET /finance/summary?from=2025-01-01&to=2025-12-31&groupBy=month|category
    [HttpGet("summary")]
    public async Task<ActionResult<FinanceSummaryDto>> GetSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? groupBy,
        CancellationToken ct)
    {
        var q = _db.Transactions.AsNoTracking()
                 .Where(t => !t.IsDeleted && t.Date >= from && t.Date <= to);

        var income = await q.Where(t => t.Type == "Ingreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var expense = await q.Where(t => t.Type == "Egreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var net = income - expense;

        // periodo anterior (mismo tamaño)
        var days = to.DayNumber - from.DayNumber + 1;
        var prevTo = from.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(days - 1));

        var qPrev = _db.Transactions.AsNoTracking()
                     .Where(t => !t.IsDeleted && t.Date >= prevFrom && t.Date <= prevTo);
        var incomePrev = await qPrev.Where(t => t.Type == "Ingreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var expensePrev = await qPrev.Where(t => t.Type == "Egreso").SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var netPrev = incomePrev - expensePrev;
        var growthPct = netPrev == 0m ? (net == 0m ? 0m : 1m) : (net - netPrev) / Math.Abs(netPrev);

        // series (sin named args en LINQ → usa posicionales)
        SeriesPointDto[] series = Array.Empty<SeriesPointDto>();
        var gb = (groupBy ?? "").ToLowerInvariant();
        if (gb == "month")
        {
            series = await q
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new SeriesPointDto(
                    $"{g.Key.Year}-{g.Key.Month:00}",
                    g.Where(t => t.Type == "Ingreso").Sum(t => t.Amount),
                    g.Where(t => t.Type == "Egreso").Sum(t => t.Amount),
                    g.Where(t => t.Type == "Ingreso").Sum(t => t.Amount) - g.Where(t => t.Type == "Egreso").Sum(t => t.Amount)
                ))
                .OrderBy(s => s.Key)
                .ToArrayAsync(ct);
        }
        else if (gb == "category")
        {
            series = await q
                .GroupBy(t => t.Category)
                .Select(g => new SeriesPointDto(
                    g.Key,
                    g.Where(t => t.Type == "Ingreso").Sum(t => t.Amount),
                    g.Where(t => t.Type == "Egreso").Sum(t => t.Amount),
                    g.Where(t => t.Type == "Ingreso").Sum(t => t.Amount) - g.Where(t => t.Type == "Egreso").Sum(t => t.Amount)
                ))
                .OrderByDescending(s => Math.Abs(s.Net))
                .ToArrayAsync(ct);
        }

        return Ok(new FinanceSummaryDto(net, income, expense, Math.Round(growthPct, 4), series));
    }

    // ------------------- LISTA (tabla) -------------------
    // GET /finance/transactions?type=&category=&clientId=&projectId=&method=&min=&max=&from=&to=&tag=&q=&page=&pageSize=
    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<TransactionDto>>> List(
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? projectId,
        [FromQuery] string? method,
        [FromQuery] decimal? min,
        [FromQuery] decimal? max,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? tag,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = _db.Transactions.AsNoTracking().Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (clientId.HasValue)
            query = query.Where(t => t.ClientId == clientId.Value);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(t => t.PaymentMethod == method);

        if (min.HasValue) query = query.Where(t => t.Amount >= min.Value);
        if (max.HasValue) query = query.Where(t => t.Amount <= max.Value);

        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagLower = tag.ToLower();
            // Tags como text[] en Postgres → usa ANY/contains
            query = query.Where(t => t.Tags != null && t.Tags.Any(x => x.ToLower().Contains(tagLower)));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            query = query.Where(t => t.Concept.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id, t.Date, t.Type, t.Category, t.Concept, t.Amount, t.Currency,
                t.PaymentMethod, t.ClientId, t.ProjectId, t.Tags
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<TransactionDto>(items, page, pageSize, total));
    }

    // ------------------- CRUD -------------------
    [HttpPost("transactions")]
    [Authorize(Roles = "Admin,Finanzas")]
    public async Task<ActionResult> Create([FromBody] TransactionCreateDto dto, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(dto.Type))
            return BadRequest(new { message = "Type inválido (Ingreso|Egreso)" });

        var entity = new Entities.Transaction
        {
            Date = dto.Date,
            Type = dto.Type,
            Category = dto.Category,
            Concept = dto.Concept.Trim(),
            Amount = dto.Amount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency,
            PaymentMethod = dto.PaymentMethod,
            ClientId = dto.ClientId,
            ProjectId = dto.ProjectId,
            Tags = dto.Tags ?? Array.Empty<string>()
        };

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
    }

    [HttpGet("transactions/{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id, CancellationToken ct)
    {
        var t = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t == null) return NotFound();

        return Ok(new TransactionDto(
            t.Id, t.Date, t.Type, t.Category, t.Concept, t.Amount, t.Currency,
            t.PaymentMethod, t.ClientId, t.ProjectId, t.Tags
        ));
    }

    [HttpPut("transactions/{id:guid}")]
    [Authorize(Roles = "Admin,Finanzas")]
    public async Task<ActionResult> Update(Guid id, [FromBody] TransactionUpdateDto dto, CancellationToken ct)
    {
        var t = await _db.Transactions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t == null) return NotFound();

        if (!AllowedTypes.Contains(dto.Type))
            return BadRequest(new { message = "Type inválido (Ingreso|Egreso)" });

        t.Date = dto.Date;
        t.Type = dto.Type;
        t.Category = dto.Category;
        t.Concept = dto.Concept.Trim();
        t.Amount = dto.Amount;
        t.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? t.Currency : dto.Currency;
        t.PaymentMethod = dto.PaymentMethod;
        t.ClientId = dto.ClientId;
        t.ProjectId = dto.ProjectId;
        t.Tags = dto.Tags ?? Array.Empty<string>();
        t.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("transactions/{id:guid}")]
    [Authorize(Roles = "Admin,Finanzas")]
    public async Task<ActionResult> SoftDelete(Guid id, [FromBody] DeleteWithReasonDto body, CancellationToken ct)
    {
        var t = await _db.Transactions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t == null) return NotFound();

        t.IsDeleted = true;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        // t.DeletedReason = body?.Reason; // si tienes el campo

        _db.AuditLogs.Add(new Entities.AuditLog
        {
            Action = "Delete",
            Entity = "Transaction",
            EntityId = id,                 // <-- antes: id.ToString() (causaba string→Guid)
            ActorUserId = Guid.Empty,      // si tu campo es string, cambia a: ActorUserId = Guid.Empty.ToString()
            At = DateTimeOffset.UtcNow,
            Diff = null
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ------------------- CATEGORIES -------------------
    [HttpGet("categories")]
    public ActionResult<IEnumerable<string>> Categories()
    {
        var cfgCats = _cfg["Finance:Categories"] ?? _cfg["Finance__Categories"];
        if (!string.IsNullOrWhiteSpace(cfgCats))
            return Ok(cfgCats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var defaults = new[] { "Mensualidad", "Venta", "Mantenimiento", "Soporte", "Compra Insumo", "Servicios Terceros", "Transporte" };

        var existing = _db.Transactions.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Select(t => t.Category)
            .Distinct()
            .ToList();

        return Ok(existing.Union(defaults).OrderBy(x => x));
    }

    // ------------------- helpers -------------------
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    { "Ingreso", "Egreso" };
}
