using System;

namespace ControlPanelGeshk.DTOs;

public record IssueCreateDto(
    string Title,
    string? Description,
    string Severity // "Low" | "Med" | "High"
);

public record IssueUpdateDto(
    string Title,
    string? Description,
    string? Severity, // opcional al actualizar
    string? Status    // "Open" | "In Progress" | "Resolved" | "Won't Fix"
);

public record IssueStatusUpdateDto(string Status);

// ÚNICA definición de IssueDto (con CreatedByName)
public record IssueDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Severity,
    string Status,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt
);
