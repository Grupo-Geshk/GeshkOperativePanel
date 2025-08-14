using System;

namespace ControlPanelGeshk.DTOs;

public record ActivityDto(
    Guid Id,
    string ScopeType,
    Guid ScopeId,
    string Type,
    DateTimeOffset OccurredAt,
    string ActorName,
    string? PayloadJson
);

public record AuditLogDto(
    Guid Id,
    string Action,
    string Entity,
    string EntityId,
    string ActorName,
    DateTimeOffset At
);

public record SearchItemDto(
    string Kind,        // "Client" | "Project" | "Meeting"
    Guid Id,
    string Title,       // nombre visible
    string? Subtitle,   // info secundaria (cliente, fecha, etc.)
    string? Extra       // url, dominio o similar
);
