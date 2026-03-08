namespace Enarro.Application.Abstractions.Cache;

/// <summary>
/// Generic cache provider abstraction. Implementations can be swapped
/// (Redis, Memcached, in-memory, etc.) without touching any consumer.
/// </summary>
public interface ICacheProvider<T> where T : class
{
    /// <summary>Returns the cached value, or null on a miss.</summary>
    Task<T?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores a value with an absolute expiry.</summary>
    Task SetAsync(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>Removes a key. No-ops if the key does not exist.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Resets the TTL on an existing key. No-ops on a miss.</summary>
    Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}