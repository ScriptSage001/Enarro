using CoreKernel.Functional.Results;
using Enarro.Application.Abstractions;
using Enarro.Application.Abstractions.Cache;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Enarro.Infrastructure.Conversation;

/// <summary>
/// Orchestrates <see cref="IConversationRepository"/> (source of truth) and
/// <see cref="ICacheProvider{T}"/> / <see cref="IListCacheProvider{T}"/> (performance layer).
///
/// Mutation flow:
///   1. Stage changes via repository (change tracker only)
///   2. Call <see cref="IUnitOfWork.SaveChangesAsync"/> once (atomically persists + fires audit interceptors)
///   3. Update caches
/// </summary>
public class ConversationStore(
    IConversationRepository repository,
    IUnitOfWork unitOfWork,
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
        UserId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString("N");

            // 1. Stage entity in change tracker
            repository.AddSession(new SessionRecord(sessionId, userId, null, default, default));

            // 2. Persist atomically — UoW sets CreatedOn / LastModifiedOn via ITimeStamped
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Warm meta cache
            await metaCache.SetAsync(
                MetaKey(sessionId),
                new CachedSessionMeta(sessionId, userId, null, DateTimeOffset.UtcNow),
                MetaExpiry,
                cancellationToken);

            logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId);
            return sessionId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session for user {UserId}", userId);
            return Result.Failure<string>(new Error("Session.CreateFailed", ex.Message, ErrorType.Unexpected));
        }
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
        try
        {
            var now = DateTimeOffset.UtcNow;

            // 1. Load the aggregate root — messages are owned by the session
            var session = await repository.GetSessionTrackedAsync(sessionId, cancellationToken);
            if (session is null)
                return Result.Failure(new Error("Session.NotFound", $"Session '{sessionId}' does not exist.", ErrorType.NotFound));

            // 2. Add message through the aggregate root
            //    This also sets LastModifiedOn, marking the session as Modified
            //    so UoW's ITimeStamped handler fires for the session too
            session.AddMessage(role, content, now);

            // 3. Persist atomically — one SaveChanges for both message + session timestamp
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Auto-generate title from the first user message
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

            // 5. Update message cache
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add message to session {SessionId}", sessionId);
            return Result.Failure(new Error("Message.AddFailed", ex.Message, ErrorType.Unexpected));
        }
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
            return Result.Failure<IReadOnlyList<ConversationMessageModel>>(
                new Error("Session.NotFound", $"Session '{sessionId}' does not exist.", ErrorType.NotFound));

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
            return Result.Failure<PagedResult<ConversationMessageModel>>(
                new Error("Session.NotFound", $"Session '{sessionId}' does not exist.", ErrorType.NotFound));

        var result = await repository.GetFullHistoryAsync(sessionId, page, pageSize, cancellationToken);
        return Result<PagedResult<ConversationMessageModel>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<SessionSummaryModel>>> GetUserSessionsAsync(
        UserId userId, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetUserSessionsAsync(userId, cancellationToken);
        return Result<IReadOnlyList<SessionSummaryModel>>.Success(result);
    }

    public async Task<Result<PagedResult<SessionSummaryModel>>> GetUserSessionsPagedAsync(
        UserId userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetUserSessionsPagedAsync(userId, page, pageSize, cancellationToken);
        return Result<PagedResult<SessionSummaryModel>>.Success(result);
    }

    public async Task<Result> UpdateSessionTitleAsync(
        string sessionId, string title,
        CancellationToken cancellationToken = default)
    {
        // Entity-level update — UoW handles LastModifiedOn automatically
        await repository.UpdateSessionTitleAsync(sessionId, title, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate meta cache — next read will repopulate with the updated title
        await metaCache.RemoveAsync(MetaKey(sessionId), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<bool>> DeleteSessionAsync(
        string sessionId, CancellationToken cancellationToken = default)
    {
        // Entity-level removal — UoW's HandleSoftDelete converts to soft-delete
        var deleted = await repository.DeleteSessionAsync(sessionId, cancellationToken);
        if (!deleted) return false;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict both cache entries
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
    UserId UserId,
    string? Title,
    DateTimeOffset CreatedAt);