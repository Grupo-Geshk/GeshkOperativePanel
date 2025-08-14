// Entities/AuditLog.cs
using System.Text.Json;
namespace ControlPanelGeshk.Entities;
public class AuditLog : BaseEntity
{
    public string Action { get; set; } = default!; // Create|Update|Delete|Reveal
    public string Entity { get; set; } = default!;
    public Guid EntityId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    public JsonDocument? Diff { get; set; }
}
