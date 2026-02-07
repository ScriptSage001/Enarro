using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Endpoint for user registration
/// </summary>
public class Register : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request, cancellationToken);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("Register")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Register a new user";
            operation.Description = "Creates a new user account with email and password";
            return operation;
        })
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);
    }
}
