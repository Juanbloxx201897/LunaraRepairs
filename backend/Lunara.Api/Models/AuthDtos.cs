namespace Lunara.Api.Models;

public record LoginDto(
    string? Email,
    string? Password,
    string? Role = null,
    string? Username = null
);
public record AdminPasswordRecoveryDto(
    string Username,
    string RecoverySecret,
    string NewPassword
);
public record LoginResponseDto(
    string Token,
    string Email,
    string Role,
    int? ClientId,
    int? TechnicianId,
    string Username,
    string DisplayName
);
