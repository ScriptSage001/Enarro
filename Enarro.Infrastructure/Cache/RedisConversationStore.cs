using System.Text.Json;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Enarro.Infrastructure.Cache;

/// <summary>
/// Redis-backed conversation store for chat sessions and history.
/// </summary>
public class RedisConversationStore(
    IConnectionMultiplexer redis,
    ILogger<RedisConversationStore> logger) : IConversationStore
{
    private static readonly TimeSpan SessionExpiry = TimeSpan.FromDays(7);
    private const string SessionPrefix = "session:";
    private const string UserSessionsPrefix = "user_sessions:";

    private IDatabase Db => redis.GetDatabase();

    public async Task<string> CreateSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");

        var sessionMeta = new SessionMetadata(sessionId, userId, DateTime.UtcNow);
        await Db.StringSetAsync(
            $"{SessionPrefix}{sessionId}:meta",
            JsonSerializer.Serialize(sessionMeta),
            SessionExpiry);

        // Track user's sessions
        await Db.SetAddAsync($"{UserSessionsPrefix}{userId}", sessionId);

        logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId);
        return sessionId;
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await Db.KeyExistsAsync($"{SessionPrefix}{sessionId}:meta");
    }

    public async Task AddMessageAsync(
        string sessionId, string role, string content, CancellationToken cancellationToken = default)
    {
        var message = new ConversationMessageModel(role, content, DateTime.UtcNow);
        await Db.ListRightPushAsync(
            $"{SessionPrefix}{sessionId}:messages",
            JsonSerializer.Serialize(message));

        // Reset TTL
        await Db.KeyExpireAsync($"{SessionPrefix}{sessionId}:messages", SessionExpiry);
    }

    public async Task<IReadOnlyList<ConversationMessageModel>> GetHistoryAsync(
        string sessionId, int maxMessages = 10, CancellationToken cancellationToken = default)
    {
        var messages = await Db.ListRangeAsync(
            $"{SessionPrefix}{sessionId}:messages",
            -maxMessages,
            -1);

        return messages
            .Select(m => JsonSerializer.Deserialize<ConversationMessageModel>(m.ToString())!)
            .ToList();
    }

    public async Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionIds = await Db.SetMembersAsync($"{UserSessionsPrefix}{userId}");
        var summaries = new List<SessionSummaryModel>();

        foreach (var sid in sessionIds)
        {
            var sessionId = sid.ToString();
            var metaJson = await Db.StringGetAsync($"{SessionPrefix}{sessionId}:meta");
            if (metaJson.IsNullOrEmpty) continue;

            var meta = JsonSerializer.Deserialize<SessionMetadata>(metaJson.ToString());
            var messageCount = await Db.ListLengthAsync($"{SessionPrefix}{sessionId}:messages");

            string? lastMessage = null;
            if (messageCount > 0)
            {
                var lastMsgJson = await Db.ListGetByIndexAsync(
                    $"{SessionPrefix}{sessionId}:messages", -1);
                if (!lastMsgJson.IsNullOrEmpty)
                {
                    var msg = JsonSerializer.Deserialize<ConversationMessageModel>(lastMsgJson.ToString());
                    lastMessage = msg?.Content;
                }
            }

            summaries.Add(new SessionSummaryModel(
                sessionId,
                meta?.CreatedAt ?? DateTime.UtcNow,
                (int)messageCount,
                lastMessage));
        }

        return summaries.OrderByDescending(s => s.CreatedAt).ToList();
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var metaJson = await Db.StringGetAsync($"{SessionPrefix}{sessionId}:meta");
        if (metaJson.IsNullOrEmpty) return false;

        var meta = JsonSerializer.Deserialize<SessionMetadata>(metaJson.ToString());

        await Db.KeyDeleteAsync($"{SessionPrefix}{sessionId}:meta");
        await Db.KeyDeleteAsync($"{SessionPrefix}{sessionId}:messages");

        if (meta is not null)
        {
            await Db.SetRemoveAsync($"{UserSessionsPrefix}{meta.UserId}", sessionId);
        }

        return true;
    }

    private record SessionMetadata(string SessionId, Guid UserId, DateTime CreatedAt);
}
