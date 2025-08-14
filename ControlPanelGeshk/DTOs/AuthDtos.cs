namespace ControlPanelGeshk.DTOs;

public record RegisterUserRequest(string Name, string Email, string Password, string? Role = "Admin");
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Name, string Email, string Role);
