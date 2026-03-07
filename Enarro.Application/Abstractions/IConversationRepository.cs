using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Conversation;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Persistence contract for conversation sessions and messages.
/// 
/// All mutating methods stage entity changes in the change tracker
/// but do NOT call SaveChanges — that is <see cref="IUnitOfWork"/>'s job.
/// This allows the caller to batch multiple repo calls into a single
/// atomic save that triggers audit / timestamp interceptors correctly.
/// </summary>
public interface IConversationRepository
{
    #region Sessions

    /// <summary>
    /// Returns a tracked session entity for mutation.
    /// Use when you need to modify the session (title, etc.).
    /// </summary>
    Task<ConversationSession?> GetSessionTrackedAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Returns a read-only projection. Use for queries / cache hydration.
    /// </summary>
    Task<SessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default);

    Task<bool> SessionExistsAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Stages a new session in the change tracker. Call UoW.SaveChangesAsync to persist.
    /// </summary>
    void AddSession(SessionRecord session);

    /// <summary>
    /// Loads the session entity and sets Title.
    /// Change is staged — call UoW.SaveChangesAsync to persist.
    /// </summary>
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken ct = default);

    /// <summary>
    /// Marks session for removal. UoW's soft-delete handler converts to soft delete.
    /// </summary>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// All sessions for a user ordered by most recently updated.
    /// </summary>
    Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(UserId userId, CancellationToken ct = default);

    /// <summary>
    /// Paginated sessions for a user ordered by most recently updated.
    /// </summary>
    Task<PagedResult<SessionSummaryModel>> GetUserSessionsPagedAsync(
        UserId userId, int page, int pageSize, CancellationToken ct = default);

    #endregion Sessions

    #region Messages

    /// <summary>
    /// Stages a new message in the change tracker. Call UoW.SaveChangesAsync to persist.
    /// </summary>
    void AddMessage(MessageRecord message);

    Task<int> GetMessageCountAsync(string sessionId, string role, CancellationToken ct = default);

    /// <summary>
    /// Returns the last <paramref name="limit"/> messages ordered ascending by time.
    /// Used for cache repopulation.
    /// </summary>
    Task<IReadOnlyList<MessageRecord>> GetRecentMessagesAsync(string sessionId, int limit, CancellationToken ct = default);

    /// <summary>
    /// Full paginated history ordered ascending by time. Used for UI history browsing.
    /// </summary>
    Task<PagedResult<ConversationMessageModel>> GetFullHistoryAsync(
        string sessionId, int page, int pageSize, CancellationToken ct = default);

    #endregion Messages
}