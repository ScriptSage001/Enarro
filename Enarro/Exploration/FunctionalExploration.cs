using CoreKernel.Functional;

namespace Enarro.Exploration;

/// <summary>
/// Exploration class to understand CoreKernel.Functional API
/// This file will be deleted after understanding the package
/// </summary>
public class FunctionalExploration
{
    public void ExploreResultType()
    {
        // Test basic Result<T> usage
        Result<string> successResult = Result<string>.Success("Hello");
        Result<string> failureResult = Result<string>.Failure("Error message");
        
        // Test Result (non-generic) usage
        Result voidSuccess = Result.Success();
        Result voidFailure = Result.Failure("Error");
        
        // Test implicit conversions
        Result<int> implicitSuccess = 42;
        Result<int> implicitFailure = Error.Create("Failed");
        
        // Test Match pattern
        var matchResult = successResult.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: error => $"Failure: {error}"
        );
        
        // Test Map/Bind operations
        var mappedResult = successResult
            .Map(s => s.ToUpper())
            .Bind(s => Result<string>.Success(s + "!"));
    }
}
