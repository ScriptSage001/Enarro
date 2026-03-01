namespace Enarro.Application.Abstractions;

/// <summary>
/// Contract for JWT token generation and validation.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token for the given user.
    /// </summary>
    string GenerateAccessToken(Guid userId, string email, string role);

    /// <summary>
    /// Generates a cryptographically secure refresh token string.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Extracts and validates the user ID from an expired access token.
    /// </summary>
    Guid? GetUserIdFromExpiredToken(string token);

    /// <summary>
    /// Gets the configured access token expiration in minutes.
    /// </summary>
    int AccessTokenExpirationMinutes { get; }

    /// <summary>
    /// Gets the configured refresh token expiration in days.
    /// </summary>
    int RefreshTokenExpirationDays { get; }
}
