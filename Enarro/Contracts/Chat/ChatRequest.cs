namespace Enarro.Contracts.Chat;

/// <summary>
/// Represents a chat request with optional session context and filtering
/// </summary>
public record ChatRequest(
    string Message,
    string? SessionId = null,
    Dictionary<string, string>? Filters = null,
    int MaxResults = 5,
    float MinRelevance = 0.3f
);
