namespace ControlPanelGeshk.DTOs;

public record EconomicsPlanCreateDto(
    string ScenarioName,
    decimal PlannedOneOffRevenue,
    decimal PlannedMonthlyRevenue,
    decimal PlannedInternalHours,
    decimal HourlyRate,
    decimal PlannedInfraMonthly,
    decimal PlannedThirdPartyMonthly,
    decimal PlannedOneOffCosts,
    string? Notes
);

public record EconomicsPlanDto(
    Guid Id,
    Guid ProjectId,
    string ScenarioName,
    decimal PlannedOneOffRevenue,
    decimal PlannedMonthlyRevenue,
    decimal PlannedInternalHours,
    decimal HourlyRate,
    decimal PlannedInfraMonthly,
    decimal PlannedThirdPartyMonthly,
    decimal PlannedOneOffCosts,
    decimal PlannedMonthlyCost,   // calculado
    decimal PlannedMargin,        // calculado (one-off + mensual estimado – costos plan)
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy
);

// Resultado de comparación plan vs real
public record MarginBreakdownDto(
    decimal OneOffRevenue,
    decimal MonthlyRevenue,
    decimal MonthlyCost,       // ManoObra + Infra + Terceros (plan o real)
    decimal OneOffCost         // Para registrar costos one-off plan/real (si aplica)
);

public record MarginCompareDto(
    EconomicsPlanDto Planned,
    MarginBreakdownDto Actual,
    MarginBreakdownDto Variance  // Actual - Planned por cada componente y margen
);
