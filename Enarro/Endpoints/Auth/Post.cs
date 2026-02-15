using Enarro.Extensions;
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
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status409Conflict);

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
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> Register(RegisterRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Login(LoginRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Refresh(RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Revoke(RevokeTokenRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await authService.RevokeTokenAsync(request.RefreshToken, request.Reason, cancellationToken);
        return result.ToHttpResult(() => Results.Ok(new { message = "Token revoked successfully" }));
    }

    #endregion Private Methods
}