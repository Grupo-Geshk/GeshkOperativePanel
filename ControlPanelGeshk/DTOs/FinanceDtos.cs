using System;

namespace ControlPanelGeshk.DTOs;

// ----- SUMMARY -----
public record FinanceSummaryDto(
    decimal Balance,
    decimal Income,
    decimal Expense,
    decimal GrowthPctVsPrevPeriod,
    SeriesPointDto[] Series
);

public record SeriesPointDto(string Key, decimal Income, decimal Expense, decimal Net);

// ----- TRANSACTIONS (tabla) -----
public record TransactionDto(
    Guid Id,
    DateOnly Date,
    string Type,
    string Category,
    string Concept,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid? ClientId,
    Guid? ProjectId,
    string[]? Tags     // <--- ANTES era string?
);

public record TransactionCreateDto(
    DateOnly Date,
    string Type,
    string Category,
    string Concept,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid? ClientId,
    Guid? ProjectId,
    string[]? Tags     // idem
);

public record TransactionUpdateDto(
    DateOnly Date,
    string Type,
    string Category,
    string Concept,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid? ClientId,
    Guid? ProjectId,
    string[]? Tags     // idem
);

public record DeleteWithReasonDto(string Reason);
