using CoreKernel.Primitives.Entities;

namespace Enarro.Domain.Conversation;

/// <summary>
/// Aggregate root representing a conversation session.
/// Owns a collection of messages.
/// </summary>
public class ConversationSession : AggregateRoot<Guid>
{
    /// <summary>
    /// External-facing session identifier (GUID string without hyphens).
    /// Used as the correlation key across cache and DB.
    /// </summary>
    public string SessionId { get; private set; } = default!;

    /// <summary>Cross-aggregate reference to the owning user.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Auto-generated or user-set title (derived from first message).</summary>
    public string? Title { get; set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; private set; } = [];

    // EF Core parameterless constructor
    private ConversationSession() { }

    public static ConversationSession Create(string sessionId, Guid userId, DateTime now) =>
        new()
        {
            SessionId = sessionId,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
}