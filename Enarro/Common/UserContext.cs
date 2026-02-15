using System.Security.Claims;

namespace Enarro.Common;

/// <summary>
/// Scoped implementation of IUserContext, populated from JWT claims via middleware.
/// </summary>
public class UserContext : IUserContext
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Populates the user context from the given ClaimsPrincipal.
    /// Called by UserContextMiddleware after authentication.
    /// </summary>
    public void Set(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return;

        IsAuthenticated = true;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            UserId = userId;
        }

        Email = principal.FindFirst(ClaimTypes.Email)?.Value;
        FirstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
        LastName = principal.FindFirst(ClaimTypes.Surname)?.Value;
        Role = principal.FindFirst(ClaimTypes.Role)?.Value;
    }
}