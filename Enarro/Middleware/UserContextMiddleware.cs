using Enarro.Common;

namespace Enarro.Middleware;

/// <summary>
/// Middleware that populates the scoped UserContext from the authenticated user's claims.
/// Must run after UseAuthentication() so that HttpContext.User is populated.
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            userContext.Set(context.User);
        }

        await _next(context);
    }
}