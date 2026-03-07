namespace Enarro.Application.Abstractions.Cache;

/// <summary>
/// Extends <see cref="ICacheProvider{T}"/> with ordered-list operations
/// needed for conversation message caching (append, trim, range).
///
/// Kept separate so simple key/value caches don't need to implement list ops.
/// </summary>
public interface IListCacheProvider<T> : ICacheProvider<T> where T : class
{
    /// <summary>Appends an item to the tail of a cached list.</summary>
    Task AppendAsync(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="count"/> items from the tail of the list
    /// (most recent). Returns an empty list on a cache miss.
    /// </summary>
    Task<IReadOnlyList<T>> GetTailAsync(string key, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the entire list with the given items and sets the TTL.
    /// Used for cache repopulation after a miss.
    /// </summary>
    Task SetListAsync(string key, IEnumerable<T> values, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>Trims the list to at most <paramref name="maxLength"/> items from the tail.</summary>
    Task TrimAsync(string key, int maxLength, CancellationToken cancellationToken = default);
}