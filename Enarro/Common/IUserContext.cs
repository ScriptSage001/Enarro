using System.Security.Claims;

namespace Enarro.Common;

/// <summary>
/// Provides access to the current authenticated user's information.
/// Populated per-request via middleware from JWT claims.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// The authenticated user's ID, or null if unauthenticated.
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// The authenticated user's email address.
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// The authenticated user's first name.
    /// </summary>
    string? FirstName { get; }
    
    /// <summary>
    /// The authenticated user's last name.
    /// </summary>
    string? LastName { get; }
    
    /// <summary>
    /// The authenticated user's role.
    /// </summary>
    string? Role { get; }
    
    /// <summary>
    /// Whether the current request is from an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Populates the user context from the given ClaimsPrincipal.
    /// Called by UserContextMiddleware after authentication.
    /// </summary>
    void Set(ClaimsPrincipal principal);
}
