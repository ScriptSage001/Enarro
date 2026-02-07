using Enarro.Models.Auth;

namespace Enarro.Services;

/// <summary>
/// Service interface for authentication and user management
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Authenticate a user and return JWT tokens
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    Task RevokeTokenAsync(string refreshToken, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user information by ID
    /// </summary>
    Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate a JWT access token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
}
