namespace Enarro.Models.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Request model for refreshing access token
/// </summary>
public record RefreshTokenRequest(
    string RefreshToken
);

/// <summary>
/// Response model for authentication operations
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

/// <summary>
/// User information DTO
/// </summary>
public record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

/// <summary>
/// Request model for revoking a refresh token
/// </summary>
public record RevokeTokenRequest(
    string RefreshToken,
    string? Reason
);
