namespace Enarro.Models.Chat;

/// <summary>
/// Represents a single message in a conversation
/// </summary>
public record ConversationMessage(
    string Role, // "user" or "assistant"
    string Content,
    DateTime Timestamp,
    List<Citation>? Citations = null,
    Dictionary<string, object>? Metadata = null
);
