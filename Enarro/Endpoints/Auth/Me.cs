using System.Security.Claims;
using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Endpoint for getting current user information
/// </summary>
public class Me : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", async (
            HttpContext httpContext,
            IAuthService authService,
            CancellationToken cancellationToken) =>
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
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get current user information";
            operation.Description = "Returns the authenticated user's profile information";
            return operation;
        })
        .Produces<UserInfo>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
