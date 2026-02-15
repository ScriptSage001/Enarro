using CoreKernel.Functional.Extensions;
using CoreKernel.Functional.Results;

namespace Enarro.Extensions;

/// <summary>
/// Extension methods for mapping Result types to HTTP IResult responses
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result{T} to an HTTP IResult, returning OK with the value on success
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: r => MapErrorToHttpResult(r.Error)
        );
    }
    
    /// <summary>
    /// Converts a non-generic Result to an HTTP IResult with OK response
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        return result.Match(
            onSuccess: () => Results.Ok(),
            onFailure: r => MapErrorToHttpResult(r.Error)
        );
    }
    
    /// <summary>
    /// Converts a Result{T} to an HTTP IResult with a custom success response
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        return result.Match(
            onSuccess: onSuccess,
            onFailure: r => MapErrorToHttpResult(r.Error)
        );
    }
    
    /// <summary>
    /// Converts a non-generic Result to an HTTP IResult with a custom success response
    /// </summary>
    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess)
    {
        return result.Match(
            onSuccess: onSuccess,
            onFailure: r => MapErrorToHttpResult(r.Error)
        );
    }

    /// <summary>
    /// Maps an Error to the appropriate HTTP status code and response using ErrorType
    /// </summary>
    private static IResult MapErrorToHttpResult(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => Results.BadRequest(new { code = error.Code, error = error.Message }),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.NotFound => Results.NotFound(new { code = error.Code, error = error.Message }),
            ErrorType.Conflict => Results.Conflict(new { code = error.Code, error = error.Message }),
            _ => Results.Problem(error.Message, statusCode: 500)
        };
    }
}
