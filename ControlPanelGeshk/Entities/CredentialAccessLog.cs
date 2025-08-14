// Entities/CredentialAccessLog.cs
using System.Net;
namespace ControlPanelGeshk.Entities;
public class CredentialAccessLog : BaseEntity
{
    public Guid CredentialId { get; set; }
    public Credential Credential { get; set; } = default!;
    public Guid ViewedBy { get; set; }
    public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
    public IPAddress? Ip { get; set; }
}
