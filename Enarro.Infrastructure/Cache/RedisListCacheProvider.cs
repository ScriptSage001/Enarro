using Enarro.Application.Abstractions.Cache;
using StackExchange.Redis;
using System.Text.Json;

namespace Enarro.Infrastructure.Cache;

/// <summary>
/// Redis-backed implementation of <see cref="IListCacheProvider{T}"/>.
/// </summary>
public class RedisListCacheProvider<T>(IConnectionMultiplexer redis) : IListCacheProvider<T>
    where T : class
{
    private IDatabase Db => redis.GetDatabase();

    /// <summary>
    /// Not applicable for list-type keys — throws <see cref="NotSupportedException"/>.
    /// </summary>
    public Task<T?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            $"{nameof(RedisListCacheProvider<>)} stores lists. Use {nameof(GetTailAsync)} instead.");

    /// <summary>
    /// Not applicable for list-type keys — throws <see cref="NotSupportedException"/>.
    /// </summary>
    public Task SetAsync(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            $"{nameof(RedisListCacheProvider<>)} stores lists. Use {nameof(AppendAsync)} or {nameof(SetListAsync)} instead.");

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await Db.KeyDeleteAsync(key);

    public async Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default) =>
        await Db.KeyExpireAsync(key, expiry);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await Db.KeyExistsAsync(key);

    public async Task AppendAsync(string key, T value, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        await Db.ListRightPushAsync(key, Serialize(value));
        await Db.KeyExpireAsync(key, expiry);
    }

    public async Task<IReadOnlyList<T>> GetTailAsync(string key, int count,
        CancellationToken cancellationToken = default)
    {
        var values = await Db.ListRangeAsync(key, -count, -1);

        return values
            .Select(v => Deserialize(v!))
            .Where(v => v is not null)
            .Cast<T>()
            .ToList();
    }

    public async Task SetListAsync(string key, IEnumerable<T> values, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var serialized = values.Select(v => (RedisValue)Serialize(v)).ToArray();
        if (serialized.Length == 0) return;

        var batch = Db.CreateBatch();
        var deleteTask = batch.KeyDeleteAsync(key);
        var pushTask = batch.ListRightPushAsync(key, serialized);
        var expireTask = batch.KeyExpireAsync(key, expiry);
        batch.Execute();

        await Task.WhenAll(deleteTask, pushTask, expireTask);
    }

    public async Task TrimAsync(string key, int maxLength,
        CancellationToken cancellationToken = default) =>
        await Db.ListTrimAsync(key, -maxLength, -1);

    #region Private Helper

    private static string Serialize(T value) => JsonSerializer.Serialize(value);
    private static T? Deserialize(string s) => JsonSerializer.Deserialize<T>(s);

    #endregion
}