namespace ControlPanelGeshk.DTOs;

public record UserOptionDto(
    Guid Id,
    string Name,
    string Role,
    bool IsActive
);
