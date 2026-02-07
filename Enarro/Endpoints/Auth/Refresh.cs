using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Endpoint for refreshing access tokens
/// </summary>
public class Refresh : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (
            RefreshTokenRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("RefreshToken")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Refresh access token";
            operation.Description = "Exchanges a refresh token for a new access token and refresh token";
            return operation;
        })
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
