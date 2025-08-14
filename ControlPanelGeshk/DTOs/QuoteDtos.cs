namespace ControlPanelGeshk.DTOs;

// --------- Lectura (lista + detalle) ----------
public record QuoteItemDto(
    Guid Id,
    string Concept,
    string Role,        // Revenue | Cost
    string Category,    // Venta, Mensualidad, ManoObra, Infra, Terceros, etc.
    decimal Quantity,
    decimal UnitPrice,
    decimal Total       // Quantity * UnitPrice
);

public record QuoteListItemDto(
    Guid Id,
    Guid ProjectId,
    string Code,
    int Version,
    string Status,          // Draft | Approved | Rejected
    string Currency,
    DateTimeOffset? ValidUntil,
    decimal? OneOffPrice,
    decimal? MonthlyFee,
    DateTimeOffset CreatedAt
);

public record QuoteDetailDto(
    Guid Id,
    Guid ProjectId,
    string Code,
    int Version,
    string Status,
    string Currency,
    DateTimeOffset? ValidUntil,
    string? Terms,
    decimal? OneOffPrice,
    decimal? MonthlyFee,
    QuoteItemDto[] Items,
    decimal ItemsRevenue,     // suma items Role=Revenue
    decimal ItemsCost,        // suma items Role=Cost
    DateTimeOffset CreatedAt,
    Guid CreatedBy
);

// --------- Escritura (crear/editar) ----------
public record QuoteItemCreateDto(
    string Concept,
    string Role,           // Revenue | Cost
    string Category,
    decimal Quantity,
    decimal UnitPrice
);

public record QuoteCreateDto(
    string? Code,          // opcional; si null => autogenerado
    string Currency,
    DateTimeOffset? ValidUntil,
    string? Terms,
    decimal? OneOffPrice,  // si envías items Revenue/Cost puedes omitir estos totales
    decimal? MonthlyFee,
    List<QuoteItemCreateDto>? Items
);

public record QuoteUpdateDto(
    string? Code,
    string? Currency,
    DateTimeOffset? ValidUntil,
    string? Terms,
    decimal? OneOffPrice,
    decimal? MonthlyFee,
    List<QuoteItemCreateDto>? Items      // reemplaza set completo
);

// --------- Acciones ----------
public record QuoteApproveDto(string? Note);
