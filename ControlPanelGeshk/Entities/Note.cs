// Entities/Note.cs
namespace ControlPanelGeshk.Entities;
public class Note : BaseEntity
{
    public string ScopeType { get; set; } = default!; // Cliente|Proyecto
    public Guid ScopeId { get; set; }
    public string Content { get; set; } = default!;
    public bool IsPinned { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public Guid? EditedBy { get; set; }
    public bool IsDeleted { get; set; }
}
