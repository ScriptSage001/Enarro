using System.Security.Claims;
using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Endpoint for revoking refresh tokens
/// </summary>
public class Revoke : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/revoke", async (
            RevokeTokenRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await authService.RevokeTokenAsync(request.RefreshToken, request.Reason, cancellationToken);
                return Results.Ok(new { message = "Token revoked successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("RevokeToken")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Revoke a refresh token";
            operation.Description = "Revokes a refresh token to prevent it from being used again";
            return operation;
        })
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
