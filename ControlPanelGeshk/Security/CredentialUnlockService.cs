using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ControlPanelGeshk.Security;

/// Emite/valida tokens efímeros (10 min) firmados con la misma clave JWT.
/// Contienen: tipo=cred-unlock, credentialId, userId.
public interface ICredentialUnlockService
{
    (string token, DateTimeOffset expires) Issue(Guid credentialId, Guid userId);
    (Guid credentialId, Guid userId)? Validate(string token);
}

public class CredentialUnlockService : ICredentialUnlockService
{
    private readonly string _key;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly int _minutes;

    public CredentialUnlockService(IConfiguration cfg)
    {
        _key = cfg["Jwt:Key"] ?? cfg["Jwt__Key"] ?? throw new InvalidOperationException("Jwt Key missing");
        _issuer = cfg["Jwt:Issuer"] ?? cfg["Jwt__Issuer"];
        _audience = cfg["Jwt:Audience"] ?? cfg["Jwt__Audience"];
        _minutes = 10; // duración del unlock
    }

    public (string token, DateTimeOffset expires) Issue(Guid credentialId, Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var exp = now.AddMinutes(_minutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("typ", "cred-unlock"),
            new Claim("cid", credentialId.ToString()),
            new Claim("uid", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var jwt = new JwtSecurityToken(_issuer, _audience, claims, notBefore: now.UtcDateTime, expires: exp.UtcDateTime, signingCredentials: creds);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, exp);
    }

    public (Guid credentialId, Guid userId)? Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parms = new TokenValidationParameters
        {
            ValidateAudience = !string.IsNullOrEmpty(_audience),
            ValidAudience = _audience,
            ValidateIssuer = !string.IsNullOrEmpty(_issuer),
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15)
        };

        var principal = handler.ValidateToken(token, parms, out _);
        if (principal.FindFirstValue("typ") != "cred-unlock") return null;

        var cid = principal.FindFirst("cid")?.Value;
        var uid = principal.FindFirst("uid")?.Value;
        if (Guid.TryParse(cid, out var credId) && Guid.TryParse(uid, out var userId))
            return (credId, userId);

        return null;
    }
}
