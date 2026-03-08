using CoreKernel.Functional.Results;
using Microsoft.AspNetCore.Mvc;

namespace Enarro.Extensions;

/// <summary>
/// Extension methods for mapping Error types to HTTP Problem Details IResult responses.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Converts an Error to the appropriate HTTP Problem Details IResult based on its ErrorType.
    /// </summary>
    public static IResult ToHttpResult(this Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.ToBadRequest(),
            ErrorType.Unauthorized => error.ToUnauthorized(),
            ErrorType.Forbidden => error.ToForbidden(),
            ErrorType.NotFound => error.ToNotFound(),
            ErrorType.Conflict => error.ToConflict(),
            _ => error.ToInternalServerError()
        };

    /// <summary>
    /// Returns a 400 Bad Request Problem Details response from this Error.
    /// </summary>
    public static IResult ToBadRequest(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            error: error);

    /// <summary>
    /// Returns a 401 Unauthorized Problem Details response from this Error.
    /// </summary>
    public static IResult ToUnauthorized(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            error: error);

    /// <summary>
    /// Returns a 403 Forbidden Problem Details response from this Error.
    /// </summary>
    public static IResult ToForbidden(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            error: error);

    /// <summary>
    /// Returns a 404 Not Found Problem Details response from this Error.
    /// </summary>
    public static IResult ToNotFound(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status404NotFound,
            title: "Not Found",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            error: error);

    /// <summary>
    /// Returns a 409 Conflict Problem Details response from this Error.
    /// </summary>
    public static IResult ToConflict(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status409Conflict,
            title: "Conflict",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            error: error);

    /// <summary>
    /// Returns a 422 Unprocessable Entity Problem Details response from this Error.
    /// </summary>
    public static IResult ToUnprocessableEntity(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status422UnprocessableEntity,
            title: "Unprocessable Entity",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            error: error);

    /// <summary>
    /// Returns a 500 Internal Server Error Problem Details response from this Error.
    /// </summary>
    public static IResult ToInternalServerError(this Error error) =>
        BuildProblem(
            status: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            error: error);


    #region Private Helpers

    private static IResult BuildProblem(int status, string title, string type, Error error)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = error.Message,
        };

        pd.Extensions["code"] = error.Code;

        return Results.Problem(pd);
    }

    #endregion
}