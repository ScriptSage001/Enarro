using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Endpoint for user login
/// </summary>
public class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.LoginAsync(request, cancellationToken);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("Login")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Login with email and password";
            operation.Description = "Authenticates a user and returns JWT access and refresh tokens";
            return operation;
        })
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
