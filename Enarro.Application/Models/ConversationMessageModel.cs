namespace Enarro.Application.Models;

/// <summary>
/// A message in a conversation session.
/// </summary>
public record ConversationMessageModel(string Role, string Content, DateTime Timestamp);
