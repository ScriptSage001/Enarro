namespace Enarro.Application.Auth.DTOs;

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt);
