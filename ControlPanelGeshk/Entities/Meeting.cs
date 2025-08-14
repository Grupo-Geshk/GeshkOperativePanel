// Entities/Meeting.cs
namespace ControlPanelGeshk.Entities;
public class Meeting : BaseEntity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = default!;
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public int? DurationMin { get; set; }
    public string Mode { get; set; } = "Presencial"; // Presencial|Remota
    public string? Location { get; set; }
    public string Status { get; set; } = "Agendada";  // Agendada|Realizada|Reprogramada|Cancelada
    public string? Notes { get; set; }
    public string? Outcomes { get; set; }
    public string[] RequiredItems { get; set; } = Array.Empty<string>();
    public Guid[] AttendeesUserIds { get; set; } = Array.Empty<Guid>();
    public bool IsDeleted { get; set; }
}
