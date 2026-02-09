namespace Enarro.Extensions;

using Enarro.Common.Errors;

/// <summary>
/// Extension methods for Result types
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result{T} to an HTTP IResult
    /// </summary>
    public static IResult ToHttpResult<T>(this CoreKernel.Functional.Result<T> result)
    {
        return result.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: error => MapErrorToHttpResult(error.Message)
        );
    }
    
    /// <summary>
    /// Converts a non-generic Result to an HTTP IResult with OK response
    /// </summary>
    public static IResult ToHttpOkResult<T>(this CoreKernel.Functional.Result<T> result)
    {
        return result.Match(
            onSuccess: _ => Results.Ok(),
            onFailure: error => MapErrorToHttpResult(error.Message)
        );
    }
    
    /// <summary>
    /// Maps an error message to the appropriate HTTP status code and response
    /// </summary>
    private static IResult MapErrorToHttpResult(string errorMessage)
    {
        // Check error message patterns to determine HTTP status code
        if (errorMessage.Contains("Invalid email or password") ||
            errorMessage.Contains("User account is inactive") ||
            errorMessage.Contains("Invalid or expired token") ||
            errorMessage.Contains("Token has expired") ||
            errorMessage.Contains("Refresh token not found") ||
            errorMessage.Contains("Unauthorized access"))
        {
            return Results.Unauthorized();
        }
        
        if (errorMessage.Contains("already registered"))
        {
            return Results.Conflict(new { error = errorMessage });
        }
        
        if (errorMessage.Contains("not found"))
        {
            return Results.NotFound(new { error = errorMessage });
        }
        
        if (errorMessage.Contains("must") ||
            errorMessage.Contains("cannot be empty") ||
            errorMessage.Contains("Invalid document format") ||
            errorMessage.Contains("exceeds maximum size"))
        {
            return Results.BadRequest(new { error = errorMessage });
        }
        
        // Default to 500 Internal Server Error
        return Results.Problem(errorMessage, statusCode: 500);
    }
}
