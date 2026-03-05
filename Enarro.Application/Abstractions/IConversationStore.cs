using Enarro.Application.Models;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Contract for conversation session and history storage.
/// Abstracts over Redis distributed cache and future storage providers.
/// </summary>
public interface IConversationStore
{
    Task<string> CreateSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        string sessionId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationMessageModel>> GetHistoryAsync(
        string sessionId,
        int maxMessages = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
