using System;

namespace ControlPanelGeshk.DTOs;

public record ProjectMiniDto(Guid Id, string Name, string Status);

public record ProjectListItemDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Name,
    string Status,
    string BillingType,
    decimal? MonthlyFee,
    decimal? OneOffFee,
    string? SiteUrl,
    bool HasGeshkSubdomain,
    string DomainController,
    DateTimeOffset StartedAt,
    DateTimeOffset? DeliveredAt
);

public record ProjectDetailDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Name,
    string Status,
    string BillingType,
    decimal? MonthlyFee,
    decimal? OneOffFee,
    string Currency,
    DomainInfo Domain,
    DateInfo Dates,
    OwnerInfo? Owner,
    NoteDto[] NotesRecent,
    IssueDto[] IssuesOpen
);

public record DomainInfo(string? SiteUrl, bool HasGeshkSubdomain, string? Subdomain, string DomainController, string? Registrar, string? HostingProvider, string? Nameservers);
public record DateInfo(DateTimeOffset StartedAt, DateTimeOffset? DueAt, DateTimeOffset? DeliveredAt);
public record OwnerInfo(Guid UserId, string Name);

public record ProjectCreateDto(
    Guid ClientId,
    string Name,
    string Status,
    string BillingType,
    decimal? MonthlyFee,
    decimal? OneOffFee,
    string? Currency,
    string? SiteUrl,
    bool HasGeshkSubdomain,
    string? Subdomain,
    string DomainController,
    string? Registrar,
    string? HostingProvider,
    string? Nameservers,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    Guid? OwnerUserId
);

public record ProjectUpdateDto(
    string Name,
    string Status,
    string BillingType,
    decimal? MonthlyFee,
    decimal? OneOffFee,
    string? Currency,
    string? SiteUrl,
    bool HasGeshkSubdomain,
    string? Subdomain,
    string DomainController,
    string? Registrar,
    string? HostingProvider,
    string? Nameservers,
    DateTimeOffset? DueAt,
    Guid? OwnerUserId
);
