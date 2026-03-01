namespace Enarro.Application.Auth.Models;

public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserModel User);

public sealed record UserModel(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt);
