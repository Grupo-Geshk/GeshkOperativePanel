using ControlPanelGeshk.Data;
using ControlPanelGeshk.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlPanelGeshk.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "Admin,Director")] // sólo quienes editan proyectos
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersController(ApplicationDbContext db) => _db = db;

    // GET /users/options?active=true&q=&role=
    // Devuelve opciones para selects (id, name, role, isActive)
    [HttpGet("options")]
    public async Task<ActionResult<IEnumerable<UserOptionDto>>> Options(
        [FromQuery] bool? active = true,
        [FromQuery] string? q = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var users = _db.Users.AsNoTracking();

        if (active.HasValue)
            users = users.Where(u => u.IsActive == active.Value);

        if (!string.IsNullOrWhiteSpace(role))
            users = users.Where(u => u.Role == role.Trim());

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            users = users.Where(u =>
                u.Name.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        var list = await users
            .OrderBy(u => u.Name)
            .Select(u => new UserOptionDto(u.Id, u.Name, u.Role, u.IsActive))
            .ToListAsync(ct);

        return Ok(list);
    }
}
