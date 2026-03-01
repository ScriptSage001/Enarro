using CoreKernel.Functional.Results;

namespace Enarro.Domain.Users;

/// <summary>
/// Domain-specific error factory for the User aggregate.
/// </summary>
public static class UserErrors
{
    public static Error InvalidCredentials() =>
        new("Auth.InvalidCredentials", "Invalid email or password", ErrorType.Unauthorized);

    public static Error UserNotFound(string identifier) =>
        new("Auth.UserNotFound", $"User '{identifier}' not found", ErrorType.NotFound);

    public static Error UserInactive() =>
        new("Auth.UserInactive", "User account is inactive", ErrorType.Unauthorized);

    public static Error EmailAlreadyExists(string email) =>
        new("Auth.EmailAlreadyExists", $"Email '{email}' is already registered", ErrorType.Conflict);

    public static Error WeakPassword(string reason) =>
        Error.Validation("Auth.WeakPassword", reason);

    public static Error InvalidToken() =>
        new("Auth.InvalidToken", "Invalid or expired token", ErrorType.Unauthorized);

    public static Error TokenExpired() =>
        new("Auth.TokenExpired", "Token has expired", ErrorType.Unauthorized);

    public static Error TokenNotFound() =>
        new("Auth.TokenNotFound", "Refresh token not found", ErrorType.NotFound);
}
