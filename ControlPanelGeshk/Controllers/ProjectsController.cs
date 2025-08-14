using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ProjectsController(ApplicationDbContext db) => _db = db;

    // GET /projects?clientId=&status=&billingType=&q=&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectListItemDto>>> List(
        [FromQuery] Guid? clientId,
        [FromQuery] string? status,
        [FromQuery] string? billingType,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = _db.Projects
            .AsNoTracking()
            .Include(p => p.Client)
            .Where(p => !p.IsDeleted);

        if (clientId.HasValue)
            query = query.Where(p => p.ClientId == clientId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(p => p.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(billingType))
        {
            var b = billingType.Trim();
            query = query.Where(p => p.BillingType == b);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.SiteUrl != null && p.SiteUrl.ToLower().Contains(s)) ||
                (p.Subdomain != null && p.Subdomain.ToLower().Contains(s)) ||
                p.Client.BusinessName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.DeliveredAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.ClientId,
                p.Client.BusinessName,
                p.Name,
                p.Status,
                p.BillingType,
                p.MonthlyFee,
                p.OneOffFee,
                p.SiteUrl,
                p.HasGeshkSubdomain,
                p.DomainController,
                p.StartedAt,
                p.DeliveredAt
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<ProjectListItemDto>(items, page, pageSize, total));
    }

    // GET /projects/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var p = await _db.Projects
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.OwnerUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (p == null) return NotFound();

        var notesRecent = await _db.Notes
            .AsNoTracking()
            .Where(n => n.ScopeType == "Proyecto" && n.ScopeId == id && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(3)
            .Select(n => new NoteDto(
                n.Id, n.ScopeType, n.ScopeId, n.Content, n.IsPinned,
                _db.Users.Where(u => u.Id == n.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                n.CreatedAt, n.EditedAt,
                n.EditedBy != null ? (_db.Users.Where(u => u.Id == n.EditedBy).Select(u => u.Name).FirstOrDefault()) : null
            ))
            .ToArrayAsync(ct);

        var issuesOpen = await _db.Issues
            .AsNoTracking()
            .Where(i => i.ProjectId == id && i.Status != "Resolved")
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new IssueDto(
                i.Id, i.ProjectId, i.Title, i.Description, i.Severity, i.Status,
                    _db.Users.Where(u => u.Id == i.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? "—",
                i.CreatedAt, i.ResolvedAt
            ))
            .ToArrayAsync(ct);

        var dto = new ProjectDetailDto(
            p.Id, p.ClientId, p.Client.BusinessName, p.Name, p.Status,
            p.BillingType, p.MonthlyFee, p.OneOffFee, p.Currency,
            new DomainInfo(p.SiteUrl, p.HasGeshkSubdomain, p.Subdomain, p.DomainController, p.Registrar, p.HostingProvider, p.Nameservers),
            new DateInfo(p.StartedAt, p.DueAt, p.DeliveredAt),
            p.OwnerUserId != null ? new OwnerInfo(p.OwnerUserId.Value, p.OwnerUser!.Name) : null,
            notesRecent,
            issuesOpen
        );

        return Ok(dto);
    }

    // POST /projects
    [HttpPost]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Create([FromBody] ProjectCreateDto dto, CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == dto.ClientId && !c.IsDeleted, ct);
        if (client == null) return BadRequest(new { message = "Cliente inválido" });
        if (dto.OwnerUserId.HasValue)
        {
            var okOwner = await _db.Users.AnyAsync(
                u => u.Id == dto.OwnerUserId.Value && u.IsActive, ct);
            if (!okOwner) return BadRequest(new { message = "OwnerUserId inválido o inactivo." });
        }
        var p = new Entities.Project
        {
            ClientId = dto.ClientId,
            Name = dto.Name.Trim(),
            Status = dto.Status,
            BillingType = dto.BillingType,
            MonthlyFee = dto.MonthlyFee,
            OneOffFee = dto.OneOffFee,
            Currency = dto.Currency ?? "USD",
            SiteUrl = dto.SiteUrl,
            HasGeshkSubdomain = dto.HasGeshkSubdomain,
            Subdomain = dto.Subdomain,
            DomainController = dto.DomainController,
            Registrar = dto.Registrar,
            HostingProvider = dto.HostingProvider,
            Nameservers = dto.Nameservers,
            StartedAt = dto.StartedAt ?? DateTimeOffset.UtcNow,
            DueAt = dto.DueAt,
            OwnerUserId = dto.OwnerUserId
        };
        
        _db.Projects.Add(p);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new { p.Id });
    }

    // PUT /projects/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Update(Guid id, [FromBody] ProjectUpdateDto dto, CancellationToken ct)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p == null) return NotFound();

        if (dto.OwnerUserId.HasValue)
        {
            var okOwner = await _db.Users.AnyAsync(
                u => u.Id == dto.OwnerUserId.Value && u.IsActive, ct);
            if (!okOwner) return BadRequest(new { message = "OwnerUserId inválido o inactivo." });
        }

        p.Name = dto.Name.Trim();
        p.Status = dto.Status;
        p.BillingType = dto.BillingType;
        p.MonthlyFee = dto.MonthlyFee;
        p.OneOffFee = dto.OneOffFee;
        p.Currency = dto.Currency ?? p.Currency;
        p.SiteUrl = dto.SiteUrl;
        p.HasGeshkSubdomain = dto.HasGeshkSubdomain;
        p.Subdomain = dto.Subdomain;
        p.DomainController = dto.DomainController;
        p.Registrar = dto.Registrar;
        p.HostingProvider = dto.HostingProvider;
        p.Nameservers = dto.Nameservers;
        p.DueAt = dto.DueAt;
        p.OwnerUserId = dto.OwnerUserId;
        p.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /projects/{id} (soft)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p == null) return NotFound();
        p.IsDeleted = true;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
