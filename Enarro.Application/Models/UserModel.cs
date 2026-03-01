namespace Enarro.Application.Models;

public sealed record UserModel(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt);
