using CoreKernel.Functional.Results;
using Enarro.Application.Abstractions;
using Enarro.Application.Abstractions.Cache;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Microsoft.Extensions.Logging;

namespace Enarro.Infrastructure.Conversation;

/// <summary>
/// Orchestrates <see cref="IConversationRepository"/> (source of truth) and
/// <see cref="ICacheProvider{T}"/> / <see cref="IListCacheProvider{T}"/> (performance layer).
///
/// No direct database or Redis calls live here — all persistence goes through
/// the repository, all caching through the cache providers.
/// </summary>
public class ConversationStore(
    IConversationRepository repository,
    ICacheProvider<CachedSessionMeta> metaCache,
    IListCacheProvider<ConversationMessageModel> messageCache,
    ILogger<ConversationStore> logger) : IConversationStore
{
    private static readonly TimeSpan MetaExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan MessageExpiry = TimeSpan.FromHours(24);

    /// <summary>
    /// Number of recent messages kept warm in cache.
    /// Sized to cover typical RAG context windows with headroom.
    /// </summary>
    private const int CachedMessageCount = 50;

    private static string MetaKey(string sessionId) => $"session:meta:{sessionId}";
    private static string MsgKey(string sessionId) => $"session:msg:{sessionId}";

    public async Task<Result<string>> CreateSessionAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        var session = new SessionRecord(sessionId, userId, null, now, now);

        // Persist first — repository is the source of truth
        await repository.AddSessionAsync(session, cancellationToken);

        // Warm meta cache
        await metaCache.SetAsync(
            MetaKey(sessionId),
            new CachedSessionMeta(sessionId, userId, null, now),
            MetaExpiry,
            cancellationToken);

        logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId);
        return sessionId;
    }

    public async Task<Result<bool>> SessionExistsAsync(
        string sessionId, CancellationToken cancellationToken = default)
    {
        if (await metaCache.ExistsAsync(MetaKey(sessionId), cancellationToken))
            return true;

        return await repository.SessionExistsAsync(sessionId, cancellationToken);
    }

    public async Task<Result> AddMessageAsync(
        string sessionId, string role, string content,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. Persist message and touch session timestamp
        await repository.AddMessageAsync(
            new MessageRecord(sessionId, role, content, now), cancellationToken);

        await repository.TouchSessionAsync(sessionId, now, cancellationToken);

        // 2. Auto-generate title from the first user message
        if (role == "user")
        {
            var userMessageCount = await repository.GetMessageCountAsync(
                sessionId, "user", cancellationToken);

            if (userMessageCount == 1)
            {
                var title = GenerateTitle(content);
                await UpdateSessionTitleAsync(sessionId, title, cancellationToken);
            }
        }

        // 3. Append to message cache, trim to window, refresh TTL
        await messageCache.AppendAsync(
            MsgKey(sessionId),
            new ConversationMessageModel(role, content, now),
            MessageExpiry,
            cancellationToken);

        await messageCache.TrimAsync(MsgKey(sessionId), CachedMessageCount, cancellationToken);

        // Refresh meta TTL so cache expiry tracks last activity
        await metaCache.RefreshAsync(MetaKey(sessionId), MetaExpiry, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ConversationMessageModel>>> GetHistoryAsync(
        string sessionId, int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        var cached = await messageCache.GetTailAsync(MsgKey(sessionId), maxMessages, cancellationToken);
        if (cached.Count > 0)
            return Result<IReadOnlyList<ConversationMessageModel>>.Success(cached);

        // Cache miss — verify session exists before hitting the database
        logger.LogDebug("Cache miss for session {SessionId}, loading from repository", sessionId);

        var exists = await repository.SessionExistsAsync(sessionId, cancellationToken);
        if (!exists)
            return Result.Failure<IReadOnlyList<ConversationMessageModel>>(new Error("Session.NotFound", $"Session '{sessionId}' does not exist.", ErrorType.NotFound));

        var messages = await repository.GetRecentMessagesAsync(
            sessionId, CachedMessageCount, cancellationToken);

        if (messages.Count > 0)
        {
            var models = messages
                .Select(m => new ConversationMessageModel(m.Role, m.Content, m.CreatedAt))
                .ToList();

            await messageCache.SetListAsync(MsgKey(sessionId), models, MessageExpiry, cancellationToken);
        }

        return messages
            .TakeLast(maxMessages)
            .Select(m => new ConversationMessageModel(m.Role, m.Content, m.CreatedAt))
            .ToList();
    }

    public async Task<Result<PagedResult<ConversationMessageModel>>> GetFullHistoryAsync(
        string sessionId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var exists = await repository.SessionExistsAsync(sessionId, cancellationToken);
        if (!exists)
            return Result.Failure<PagedResult<ConversationMessageModel>>(new Error("Session.NotFound", $"Session '{sessionId}' does not exist.", ErrorType.NotFound));

        var result = await repository.GetFullHistoryAsync(sessionId, page, pageSize, cancellationToken);
        return Result<PagedResult<ConversationMessageModel>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<SessionSummaryModel>>> GetUserSessionsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetUserSessionsAsync(userId, cancellationToken);
        return Result<IReadOnlyList<SessionSummaryModel>>.Success(result);
    }

    public async Task<Result<PagedResult<SessionSummaryModel>>> GetUserSessionsPagedAsync(
        Guid userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetUserSessionsPagedAsync(userId, page, pageSize, cancellationToken);
        return Result<PagedResult<SessionSummaryModel>>.Success(result);
    }

    public async Task<Result> UpdateSessionTitleAsync(
        string sessionId, string title,
        CancellationToken cancellationToken = default)
    {
        await repository.UpdateSessionTitleAsync(sessionId, title, cancellationToken);

        // Invalidate meta cache — next read will repopulate with the updated title
        await metaCache.RemoveAsync(MetaKey(sessionId), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<bool>> DeleteSessionAsync(
        string sessionId, CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteSessionAsync(sessionId, cancellationToken);
        if (!deleted) return false;

        // Evict both cache entries — cascade delete in PostgreSQL handles messages
        await metaCache.RemoveAsync(MetaKey(sessionId), cancellationToken);
        await messageCache.RemoveAsync(MsgKey(sessionId), cancellationToken);

        return true;
    }

    #region Private Helper

    /// <summary>
    /// Derives a short title from the first user message.
    /// Truncates at the first sentence boundary or 80 characters.
    /// </summary>
    private static string GenerateTitle(string firstMessage)
    {
        var trimmed = firstMessage.Trim();
        var sentenceEnd = trimmed.IndexOfAny(['.', '?', '!']);

        if (sentenceEnd > 0 && sentenceEnd <= 80)
            return trimmed[..(sentenceEnd + 1)];

        return trimmed.Length <= 80 ? trimmed : trimmed[..77] + "...";
    }

    #endregion
}

/// <summary>
/// Slim metadata record stored in the Redis meta cache.
/// </summary>
public record CachedSessionMeta(
    string SessionId,
    Guid UserId,
    string? Title,
    DateTime CreatedAt);