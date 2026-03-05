using Enarro.Application.Abstractions;
using Enarro.Application.Auth.Commands;
using Enarro.Extensions;
using Enarro.Contracts.Auth;
using MediatR;

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
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status409Conflict);

        app.MapPost("/auth/login", Login)
            .WithName("Login")
            .WithTags("Authentication")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/auth/refresh", Refresh)
            .WithName("RefreshToken")
            .WithTags("Authentication")
            .Produces<object>(StatusCodes.Status200OK)
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

    private static async Task<IResult> Register(RegisterRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Login(LoginRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Refresh(RefreshTokenRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Revoke(RevokeTokenRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RevokeTokenCommand(request.RefreshToken, request.Reason);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(() => Results.Ok(new { message = "Token revoked successfully" }));
    }

    #endregion Private Methods
}