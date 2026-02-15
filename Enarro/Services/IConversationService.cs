using CoreKernel.Functional.Results;
using Enarro.Models.Chat;
using Enarro.Models.Session;

namespace Enarro.Services;

/// <summary>
/// Service for managing conversation history and sessions
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Creates a new session
    /// </summary>
    Task<Result<string>> CreateSessionAsync(string? userId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves conversation history for a session
    /// </summary>
    Task<Result<List<ConversationMessage>>> GetHistoryAsync(string sessionId, int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a message to the conversation history
    /// </summary>
    Task<Result> AddMessageAsync(string sessionId, ConversationMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all messages in a session
    /// </summary>
    Task<Result> ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a session exists
    /// </summary>
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets session information
    /// </summary>
    Task<Result<SessionInfo>> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default);
}
