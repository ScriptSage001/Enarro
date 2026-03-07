using CoreKernel.Primitives.Entities;

namespace Enarro.Domain.Conversation;

/// <summary>
/// Child entity belonging to a <see cref="ConversationSession"/>.
/// </summary>
public class ConversationMessage : Entity<long>
{
    public string SessionId { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    /// <summary>Navigation property back to the owning session.</summary>
    public ConversationSession Session { get; private set; } = default!;

    // EF Core parameterless constructor
    private ConversationMessage() { }

    public static ConversationMessage Create(string sessionId, string role, string content, DateTime createdAt) =>
        new()
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = createdAt
        };
}