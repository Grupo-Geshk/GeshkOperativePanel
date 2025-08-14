// Entities/Transaction.cs
namespace ControlPanelGeshk.Entities;
public class Transaction : BaseEntity
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Type { get; set; } = "Ingreso"; // Ingreso|Egreso
    public string Category { get; set; } = default!;
    public string Concept { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "Efectivo";
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string? AttachmentUrl { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Guid CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public string? DeletedReason { get; set; }
}
