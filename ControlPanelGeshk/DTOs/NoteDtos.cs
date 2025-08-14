using System;

namespace ControlPanelGeshk.DTOs;

public record NoteCreateDto(
    string ScopeType,   // "Cliente" | "Proyecto"
    Guid ScopeId,
    string Content,
    bool IsPinned = false
);

public record NoteUpdateDto(
    string Content,
    bool IsPinned
);

public record NotePinUpdateDto(bool IsPinned);

// ÚNICA definición de NoteDto (con nombres claros)
public record NoteDto(
    Guid Id,
    string ScopeType,
    Guid ScopeId,
    string Content,
    bool IsPinned,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    string? EditedByName
);
