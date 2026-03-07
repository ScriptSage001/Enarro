namespace Enarro.Application.Models;

/// <summary>
/// Summary of a conversation session for sidebar/listing display.
/// </summary>
public record SessionSummaryModel(
    string SessionId,
    string? Title,
    DateTime CreatedAt,
    int MessageCount,
    string? LastMessage);
