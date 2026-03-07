namespace Enarro.Application.Models;

/// <summary>
/// Summary of a conversation session for sidebar/listing display.
/// </summary>
public record SessionSummaryModel(
    string SessionId,
    string? Title,
    DateTimeOffset CreatedAt,
    int MessageCount,
    string? LastMessage);
