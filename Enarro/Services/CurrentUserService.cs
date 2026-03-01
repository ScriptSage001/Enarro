using System.Security.Claims;
using Enarro.Application.Abstractions;

namespace Enarro.Services;

/// <summary>
/// Implements ICurrentUserService by reading claims from HttpContext.
/// Registered as scoped and populated by UserContextMiddleware.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Populates the service from the given ClaimsPrincipal.
    /// Called by UserContextMiddleware after authentication.
    /// </summary>
    public void SetFromPrincipal(ClaimsPrincipal principal)
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
        Role = principal.FindFirst(ClaimTypes.Role)?.Value;
    }
}
