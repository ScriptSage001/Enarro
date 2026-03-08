using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.DomainMarkers.SoftDeletion;
using CoreKernel.Primitives.Entities;
using Enarro.Domain.Common;

namespace Enarro.Domain.Conversation;

/// <summary>
/// Aggregate root representing a conversation session.
/// Owns a collection of messages — all message mutations go through here.
/// </summary>
public class ConversationSession : AggregateRoot<Guid>, ITimeStamped, ISoftDeletable
{
    /// <summary>
    /// External-facing session identifier (GUID string without hyphens).
    /// Used as the correlation key across cache and DB.
    /// </summary>
    public string SessionId { get; private set; } = default!;

    /// <summary>
    /// Cross-aggregate reference to the owning user.
    /// </summary>
    public UserId UserId { get; private set; } = default!;

    /// <summary>
    /// Auto-generated or user-set title (derived from first message).
    /// </summary>
    public string? Title { get; set; }


    // ITimeStamped
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastModifiedOn { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    public ICollection<ConversationMessage> Messages { get; private set; } = [];

    // EF Core parameterless constructor
    private ConversationSession() { }

    public static ConversationSession Create(string sessionId, UserId userId, string? title) =>
        new()
        {
            SessionId = sessionId,
            UserId = userId,
            Title = title
        };

    /// <summary>
    /// Adds a message through the aggregate root.
    /// Writing to <see cref="LastModifiedOn"/> marks this entity as Modified
    /// in the change tracker, so UoW's <c>ITimeStamped</c> handler will
    /// overwrite it with the precise save timestamp.
    /// </summary>
    public ConversationMessage AddMessage(string role, string content, DateTimeOffset createdAt)
    {
        var message = ConversationMessage.Create(SessionId, role, content, createdAt);
        Messages.Add(message);
        LastModifiedOn = createdAt;
        return message;
    }
}