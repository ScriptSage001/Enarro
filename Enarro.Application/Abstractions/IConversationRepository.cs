using Enarro.Application.Common;
using Enarro.Application.Models;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Persistence contract for conversation sessions and messages.
/// Consumed by <see cref="IConversationStore"/> implementations — never called
/// directly from application handlers.
/// </summary>
public interface IConversationRepository
{
    // ─── Sessions ────────────────────────────────────────────────────────

    Task<SessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken ct = default);
    Task AddSessionAsync(SessionRecord session, CancellationToken ct = default);
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken ct = default);
    Task TouchSessionAsync(string sessionId, DateTime updatedAt, CancellationToken ct = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>All sessions for a user ordered by most recently updated.</summary>
    Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Paginated sessions for a user ordered by most recently updated.</summary>
    Task<PagedResult<SessionSummaryModel>> GetUserSessionsPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    // ─── Messages ────────────────────────────────────────────────────────

    Task AddMessageAsync(MessageRecord message, CancellationToken ct = default);
    Task<int> GetMessageCountAsync(string sessionId, string role, CancellationToken ct = default);

    /// <summary>
    /// Returns the last <paramref name="limit"/> messages ordered ascending by time.
    /// Used for cache repopulation.
    /// </summary>
    Task<IReadOnlyList<MessageRecord>> GetRecentMessagesAsync(string sessionId, int limit, CancellationToken ct = default);

    /// <summary>Full paginated history ordered ascending by time. Used for UI history browsing.</summary>
    Task<PagedResult<ConversationMessageModel>> GetFullHistoryAsync(
        string sessionId, int page, int pageSize, CancellationToken ct = default);
}

// ─── Transfer records between repo and store layers ─────────────────────

public record SessionRecord(
    string SessionId,
    Guid UserId,
    string? Title,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MessageRecord(
    string SessionId,
    string Role,
    string Content,
    DateTime CreatedAt);
