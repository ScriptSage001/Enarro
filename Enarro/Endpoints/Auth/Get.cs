using Enarro.Models.Auth;
using Enarro.Services;
using System.Security.Claims;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Get Endpoints for Authentication
/// </summary>
public class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", Me)
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithTags("Authentication")
        .Produces<UserInfo>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    #region Private Methods

    private async Task<IResult> Me(HttpContext httpContext, IAuthService authService, CancellationToken cancellationToken)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var userInfo = await authService.GetUserByIdAsync(userId, cancellationToken);

        if (userInfo == null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        return Results.Ok(userInfo);
    }

    #endregion Private Methods
}
