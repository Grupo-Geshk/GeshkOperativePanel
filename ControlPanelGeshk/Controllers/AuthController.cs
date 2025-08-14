using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using ControlPanelGeshk.Entities;
using ControlPanelGeshk.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _cfg;

    public AuthController(ApplicationDbContext db, IPasswordHasher hasher, IConfiguration cfg)
    {
        _db = db; _hasher = hasher; _cfg = cfg;
    }

    // REGISTRO (por ahora abierto; cuando terminen de crear los 3 admins, conviene
    // poner [Authorize(Roles="Admin")] o deshabilitar este endpoint).
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterUserRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { message = "El correo ya está registrado." });

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = email,
            Role = string.IsNullOrWhiteSpace(req.Role) ? "Admin" : req.Role!,
            PasswordHash = _hasher.Hash(req.Password),
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Register), new { id = user.Id }, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.IsActive
        });
    }

    // LOGIN: devuelve solo AccessToken (JWT) + datos básicos.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
        if (user == null) return Unauthorized(new { message = "Credenciales inválidas" });

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        var (token, exp) = IssueJwt(user);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse(token, exp, user.Name, user.Email, user.Role));
    }

    // ----------------- helpers -----------------
    private (string token, DateTimeOffset expires) IssueJwt(User user)
    {
        var key = _cfg["Jwt:Key"] ?? _cfg["Jwt__Key"]
                  ?? throw new InvalidOperationException("Jwt__Key faltante.");
        var issuer = _cfg["Jwt:Issuer"] ?? _cfg["Jwt__Issuer"];
        var audience = _cfg["Jwt:Audience"] ?? _cfg["Jwt__Audience"];
        var minutes = int.TryParse(_cfg["Jwt:AccessMinutes"] ?? _cfg["Jwt__AccessMinutes"], out var m) ? m : 60;

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var exp = now.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: exp.UtcDateTime,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, exp);
    }
}
