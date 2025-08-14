using System.Linq;
using System.Security.Claims;
using System.Text; // <-- para Encoding.UTF8
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using ControlPanelGeshk.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("credentials")]
[Authorize]
public class CredentialsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISecretCrypto _crypto;
    private readonly ICredentialUnlockService _unlock;
    private readonly IConfiguration _cfg;

    public CredentialsController(ApplicationDbContext db, ISecretCrypto crypto, ICredentialUnlockService unlock, IConfiguration cfg)
    {
        _db = db; _crypto = crypto; _unlock = unlock; _cfg = cfg;
    }

    // LISTA por proyecto (solo metadatos; nunca el secreto)
    // GET /projects/{projectId}/credentials
    [HttpGet("/projects/{projectId:guid}/credentials")]
    public async Task<ActionResult<IReadOnlyList<CredentialMetaDto>>> ListByProject(Guid projectId, CancellationToken ct)
    {
        var meta = await _db.Credentials.AsNoTracking()
            .Where(c => c.ScopeType == "Proyecto" && c.ScopeId == projectId && !c.IsArchived)
            .OrderBy(c => c.Kind).ThenBy(c => c.Username)
            .Select(c => new CredentialMetaDto(
                c.Id,
                c.ScopeType,
                c.ScopeId,
                c.Kind,
                c.Username,
                c.Url,
                c.LastRotatedAt,
                c.UpdatedAt ?? c.CreatedAt // <-- evita error por DateTimeOffset? -> DateTimeOffset
            ))
            .ToListAsync(ct);

        return Ok(meta);
    }

    // CREAR
    [HttpPost]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Create([FromBody] CredentialCreateDto dto, CancellationToken ct)
    {
        if (dto.ScopeType != "Proyecto" && dto.ScopeType != "Cliente")
            return BadRequest(new { message = "ScopeType inválido (Proyecto|Cliente)" });

        var scopeOk = dto.ScopeType == "Proyecto"
            ? await _db.Projects.AnyAsync(p => p.Id == dto.ScopeId /* && !p.IsDeleted*/, ct)
            : await _db.Clients.AnyAsync(c => c.Id == dto.ScopeId /* && !c.IsDeleted*/, ct);

        if (!scopeOk) return BadRequest(new { message = "ScopeId inválido" });

        // _crypto.Encrypt devuelve string -> tu entidad guarda byte[]:
        var encryptedText = _crypto.Encrypt(dto.SecretPlain);
        var entity = new Entities.Credential
        {
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Kind = dto.Kind,
            Username = dto.Username,
            SecretEncrypted = Encoding.UTF8.GetBytes(encryptedText), // <-- string -> byte[]
            Url = dto.Url,
            Notes = dto.Notes,
            LastRotatedAt = DateTimeOffset.UtcNow,
            CreatedBy = GetUserId()
        };
        _db.Credentials.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetMeta), new { id = entity.Id }, new { entity.Id });
    }

    // METADATA de una credencial
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CredentialMetaDto>> GetMeta(Guid id, CancellationToken ct)
    {
        var c = await _db.Credentials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null) return NotFound();

        return Ok(new CredentialMetaDto(
            c.Id, c.ScopeType, c.ScopeId, c.Kind, c.Username, c.Url, c.LastRotatedAt, c.UpdatedAt ?? c.CreatedAt
        ));
    }

    // UNLOCK: valida passphrase y devuelve token efímero (10 min)
    [HttpPost("{id:guid}/unlock")]
    public async Task<ActionResult<CredentialUnlockResponse>> Unlock(Guid id, [FromBody] CredentialUnlockRequest req, CancellationToken ct)
    {
        var cred = await _db.Credentials.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cred == null) return NotFound();

        var pass = _cfg["Credentials:Passphrase"] ?? _cfg["Credentials__Passphrase"];
        if (string.IsNullOrWhiteSpace(pass))
            return StatusCode(500, new { message = "No está configurada la segunda clave (Credentials__Passphrase)." });

        if (!string.Equals(req.Passphrase, pass, StringComparison.Ordinal))
            return Unauthorized(new { message = "Passphrase inválida." });

        var (token, exp) = _unlock.Issue(id, GetUserId());
        return Ok(new CredentialUnlockResponse(token, exp));
    }

    // REVEAL: requiere unlockToken válido. Registra AccessLog.
    [HttpGet("{id:guid}/reveal")]
    public async Task<ActionResult<CredentialRevealDto>> Reveal(Guid id, [FromQuery] string unlockToken, [FromQuery] string? reason, CancellationToken ct)
    {
        var tuple = _unlock.Validate(unlockToken);
        if (tuple == null || tuple.Value.credentialId != id)
            return Unauthorized(new { message = "Unlock token inválido." });

        var cred = await _db.Credentials.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cred == null) return NotFound();

        // Tu entidad guarda byte[] -> pasa a string antes de desencriptar:
        var encryptedText = Encoding.UTF8.GetString(cred.SecretEncrypted); // byte[] -> string
        var secret = _crypto.Decrypt(encryptedText);                        // string -> string (plaintext)

        var dto = new CredentialRevealDto(cred.Id, cred.Kind, cred.Username, secret, cred.Url, cred.Notes);

        _db.CredentialAccessLogs.Add(new Entities.CredentialAccessLog
        {
            CredentialId = cred.Id,
            ViewedBy = tuple.Value.userId,
            ViewedAt = DateTimeOffset.UtcNow,
            Reason = reason
        });
        await _db.SaveChangesAsync(ct);

        return Ok(dto);
    }

    // UPDATE (incluye rotación opcional) — requiere unlockToken
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult> Update(Guid id, [FromBody] CredentialUpdateDto dto, [FromQuery] string unlockToken, CancellationToken ct)
    {
        var tuple = _unlock.Validate(unlockToken);
        if (tuple == null || tuple.Value.credentialId != id)
            return Unauthorized(new { message = "Unlock token inválido." });

        var c = await _db.Credentials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null) return NotFound();

        c.Username = dto.Username ?? c.Username;
        c.Url = dto.Url ?? c.Url;
        c.Notes = dto.Notes ?? c.Notes;

        if (dto.IsArchived.HasValue) c.IsArchived = dto.IsArchived.Value;

        if (!string.IsNullOrWhiteSpace(dto.SecretPlain))
        {
            var encryptedText = _crypto.Encrypt(dto.SecretPlain);          // string
            c.SecretEncrypted = Encoding.UTF8.GetBytes(encryptedText);     // string -> byte[]
            c.LastRotatedAt = DateTimeOffset.UtcNow;
        }

        c.UpdatedAt = DateTimeOffset.UtcNow;
        c.UpdatedBy = tuple.Value.userId;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
