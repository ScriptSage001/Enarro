using CoreKernel.Functional.Results;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Common;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Contract for conversation session and history storage.
/// Abstracts over Redis distributed cache and future storage providers.
/// </summary>
public interface IConversationStore
{
    Task<Result<string>> CreateSessionAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<Result<bool>> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<Result> AddMessageAsync(
        string sessionId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ConversationMessageModel>>> GetHistoryAsync(
        string sessionId,
        int maxMessages = 10,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SessionSummaryModel>>> GetUserSessionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated session list for the UI sidebar.
    /// </summary>
    Task<Result<PagedResult<SessionSummaryModel>>> GetUserSessionsPagedAsync(
        UserId userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full paginated message history for history browsing.
    /// Always reads from the repository — not the cache.
    /// </summary>
    Task<Result<PagedResult<ConversationMessageModel>>> GetFullHistoryAsync(
        string sessionId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly set or update a session title.
    /// </summary>
    Task<Result> UpdateSessionTitleAsync(
        string sessionId, string title,
        CancellationToken cancellationToken = default);
}
