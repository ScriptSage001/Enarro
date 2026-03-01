namespace Enarro.Application.Models;

/// <summary>
/// Summary of a conversation session.
/// </summary>
public record SessionSummaryModel(string SessionId, DateTime CreatedAt, int MessageCount, string? LastMessage);
