// Entities/Project.cs
namespace ControlPanelGeshk.Entities;
public class Project : BaseEntity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = "En Proceso";  // Activo|En Proceso|En Pausa|Entregado|Cerrado
    public string BillingType { get; set; } = "Único";  // Suscripción|Único
    public decimal? MonthlyFee { get; set; }
    public decimal? OneOffFee { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset? NextChargeAt { get; set; }

    public string? SiteUrl { get; set; }
    public bool HasGeshkSubdomain { get; set; }
    public string? Subdomain { get; set; }
    public string DomainController { get; set; } = "Cliente"; // Cliente|GESHK|Tercero
    public string? Registrar { get; set; }
    public string? HostingProvider { get; set; }
    public string? Nameservers { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
