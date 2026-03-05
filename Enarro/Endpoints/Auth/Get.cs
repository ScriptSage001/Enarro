using Enarro.Application.Abstractions;
using Enarro.Application.Auth.Queries;
using Enarro.Extensions;
using MediatR;

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
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    #region Private Methods

    private static async Task<IResult> Me(ICurrentUserService currentUser, ISender sender, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserQuery(currentUser.UserId.Value);
        var result = await sender.Send(query, cancellationToken);
        return result.ToHttpResult();
    }

    #endregion Private Methods
}