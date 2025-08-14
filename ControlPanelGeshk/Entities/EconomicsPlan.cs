// Entities/EconomicsPlan.cs
namespace ControlPanelGeshk.Entities;
public class EconomicsPlan : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public string ScenarioName { get; set; } = default!;
    public decimal PlannedOneOffRevenue { get; set; }
    public decimal PlannedMonthlyRevenue { get; set; }
    public decimal PlannedInternalHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal PlannedInfraMonthly { get; set; }
    public decimal PlannedThirdPartyMonthly { get; set; }
    public decimal PlannedOneOffCosts { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
}
