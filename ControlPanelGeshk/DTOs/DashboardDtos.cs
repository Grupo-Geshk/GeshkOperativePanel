namespace ControlPanelGeshk.DTOs;

public record DashboardSummaryDto(
    int NClients,
    int ActiveOrders,
    int ServicesDelivered,
    decimal Revenue,
    List<Guid> LastServices
);
