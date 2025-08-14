// Entities/User.cs
namespace ControlPanelGeshk.Entities;
public class User : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = "Admin"; // Admin|Director|Operativo|Finanzas
    public string PasswordHash { get; set; } = default!;
    public bool TotpEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
}
