using Enarro.Application.Abstractions.Cache;
using StackExchange.Redis;
using System.Text.Json;

namespace Enarro.Infrastructure.Cache;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheProvider{T}"/> for scalar values.
/// Used for session metadata caching.
/// </summary>
public class RedisCacheProvider<T>(IConnectionMultiplexer redis) : ICacheProvider<T>
    where T : class
{
    private IDatabase Db => redis.GetDatabase();

    public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync(string key, T value, TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        await Db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry);

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await Db.KeyDeleteAsync(key);

    public async Task RefreshAsync(string key, TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        await Db.KeyExpireAsync(key, expiry);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await Db.KeyExistsAsync(key);
}