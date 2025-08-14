// Entities/Activity.cs
using System.Text.Json;
namespace ControlPanelGeshk.Entities;
public class Activity : BaseEntity
{
    public string ScopeType { get; set; } = default!; // Cliente|Proyecto
    public Guid ScopeId { get; set; }
    public string Type { get; set; } = default!;      // MeetingScheduled|...
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
