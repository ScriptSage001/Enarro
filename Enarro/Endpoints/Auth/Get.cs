using Enarro.Common;
using Enarro.Extensions;
using Enarro.Models.Auth;
using Enarro.Services;

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

    private static async Task<IResult> Me(IUserContext userContext, IAuthService authService, CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await authService.GetUserByIdAsync(userContext.UserId.Value, cancellationToken);
        return result.ToHttpResult();
    }

    #endregion Private Methods
}