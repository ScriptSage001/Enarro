namespace Enarro.Contracts.Chat;

/// <summary>
/// Represents a chat response with citations and metadata
/// </summary>
public record ChatResponse(
    string Answer,
    List<Citation> Citations,
    double ConfidenceScore,
    string SessionId,
    int TokensUsed);