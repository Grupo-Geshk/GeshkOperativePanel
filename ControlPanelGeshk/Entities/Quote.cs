// Entities/Quote.cs
namespace ControlPanelGeshk.Entities;
public class Quote : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public string Code { get; set; } = default!;
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Draft"; // Draft|Approved|Rejected
    public string Currency { get; set; } = "USD";
    public DateOnly? ValidUntil { get; set; }
    public string? Terms { get; set; }
    public decimal? OneOffPrice { get; set; }
    public decimal? MonthlyFee { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
public class QuoteItem : BaseEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = default!;
    public string Concept { get; set; } = default!;
    public string Role { get; set; } = "Revenue"; // Revenue|Cost
    public string Category { get; set; } = "Venta"; // Venta|Mensualidad|ManoObra|Infra|Terceros|Otro
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public decimal Total => Quantity * UnitPrice;
}
