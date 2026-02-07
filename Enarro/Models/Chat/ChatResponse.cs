namespace Enarro.Models.Chat;

/// <summary>
/// Represents a chat response with citations and metadata
/// </summary>
public record ChatResponse(
    string Answer,
    List<Citation> Citations,
    float Confidence,
    string SessionId,
    int TokensUsed,
    DateTime Timestamp
);
