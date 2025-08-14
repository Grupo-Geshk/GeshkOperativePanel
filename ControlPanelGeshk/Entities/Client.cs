// Entities/Client.cs
namespace ControlPanelGeshk.Entities;
public class Client : BaseEntity
{
    public string BusinessName { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? NotesBrief { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
