namespace Enarro.Contracts.Common;

/// <summary>
/// Generic paged result for list operations
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
