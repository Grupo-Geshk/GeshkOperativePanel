using System;

namespace ControlPanelGeshk.DTOs;

public record ClientListItemDto(
    Guid Id,
    string BusinessName,
    string ClientName,
    string? Phone,
    string? Email,
    string? Location,
    int ProjectCount
);

public record ClientDetailDto(
    Guid Id,
    string BusinessName,
    string ClientName,
    string? Phone,
    string? Email,
    string? Location,
    string? NotesBrief,
    int Projects,
    int OpenIssues,
    DateTimeOffset? LastDeliveryAt,
    DateOnly? LastPaymentDate,
    ProjectMiniDto[] ClientProjects
);

public record ClientCreateDto(string BusinessName, string ClientName, string? Phone, string? Email, string? Location, string? NotesBrief);
public record ClientUpdateDto(string BusinessName, string ClientName, string? Phone, string? Email, string? Location, string? NotesBrief);
