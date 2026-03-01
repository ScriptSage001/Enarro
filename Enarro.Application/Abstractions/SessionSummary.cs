namespace Enarro.Application.Abstractions;

/// <summary>
/// Summary of a conversation session.
/// </summary>
public record SessionSummary(string SessionId, DateTime CreatedAt, int MessageCount, string? LastMessage);
