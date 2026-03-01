namespace Enarro.Application.Abstractions;

/// <summary>
/// A message in a conversation session.
/// </summary>
public record ConversationMessage(string Role, string Content, DateTime Timestamp);
