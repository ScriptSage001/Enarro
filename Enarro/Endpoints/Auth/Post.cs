using Enarro.Models.Auth;
using Enarro.Services;

namespace Enarro.Endpoints.Auth;

/// <summary>
/// Post Endpoints for Authentication
/// </summary>
public class Post : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", Register)
            .WithName("Register")
            .WithTags("Authentication")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest);

        app.MapPost("/auth/login", Login)
            .WithName("Login")
            .WithTags("Authentication")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/auth/refresh", Refresh)
            .WithName("RefreshToken")
            .WithTags("Authentication")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/auth/revoke", Revoke)
            .RequireAuthorization()
            .WithName("RevokeToken")
            .WithTags("Authentication")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    #endregion Public Endpoints

    #region Private Methods

    private async Task<IResult> Register(RegisterRequest request, IAuthService authService, CancellationToken cancellationToken)
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
    }

    private async Task<IResult> Login(LoginRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private async Task<IResult> Refresh(RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private async Task<IResult> Revoke(RevokeTokenRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken)
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
    }

    #endregion Private Methods
}