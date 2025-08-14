using System;

namespace ControlPanelGeshk.DTOs;

public record CredentialMetaDto(
    Guid Id,
    string ScopeType,   // "Proyecto" o "Cliente" (hoy usaremos Proyecto)
    Guid ScopeId,
    string Kind,        // Registrar | Hosting | cPanel | Cloudflare | AdminApp | Email | Otro
    string? Username,
    string? Url,
    DateTimeOffset? LastRotatedAt,
    DateTimeOffset UpdatedAt
);

public record CredentialCreateDto(
    string ScopeType,   // "Proyecto" | "Cliente"
    Guid ScopeId,
    string Kind,
    string? Username,
    string SecretPlain,
    string? Url,
    string? Notes
);

public record CredentialUpdateDto(
    string? Username,
    string? SecretPlain, // si viene, rota el secreto
    string? Url,
    string? Notes,
    bool? IsArchived
);

public record CredentialUnlockRequest(string Passphrase /* segunda clave */);
public record CredentialUnlockResponse(string UnlockToken, DateTimeOffset ExpiresAt);

public record CredentialRevealDto(
    Guid Id,
    string Kind,
    string? Username,
    string SecretPlain,
    string? Url,
    string? Notes
);
