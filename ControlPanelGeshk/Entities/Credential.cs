// Entities/Credential.cs
namespace ControlPanelGeshk.Entities;
public class Credential : BaseEntity
{
    public string ScopeType { get; set; } = default!; // Cliente|Proyecto
    public Guid ScopeId { get; set; }
    public string Kind { get; set; } = default!;      // Registrar|Hosting|cPanel|Cloudflare|AdminApp|Email|Otro
    public string? Username { get; set; }
    public byte[] SecretEncrypted { get; set; } = Array.Empty<byte>();
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? LastRotatedAt { get; set; }
    public bool IsArchived { get; set; }
    public int Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
