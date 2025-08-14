// Entities/Issue.cs
namespace ControlPanelGeshk.Entities;
public class Issue : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Severity { get; set; } = "Low";      // Low|Med|High
    public string Status { get; set; } = "Open";       // Open|In Progress|Resolved|Won’t Fix
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
