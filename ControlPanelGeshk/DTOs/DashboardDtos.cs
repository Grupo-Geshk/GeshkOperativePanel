// DashboardDtos.cs
namespace ControlPanelGeshk.DTOs;

public record DashboardSummaryDto(
    int NClients,
    int ActiveOrders,
    int ServicesDelivered,
    decimal Revenue,     // ingresos del rango
    decimal Expense,     // egresos del rango  <-- NUEVO
    decimal Net,         // Revenue - Expense  <-- NUEVO
    List<Guid> LastServices
);
