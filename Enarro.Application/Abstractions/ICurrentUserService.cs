namespace Enarro.Application.Abstractions;

/// <summary>
/// Contract for accessing the current authenticated user's context.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
