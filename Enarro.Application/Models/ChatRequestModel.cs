namespace Enarro.Application.Models;

/// <summary>
/// Represents a chat request with optional session context and filtering
/// </summary>
public record ChatRequestModel(
    string Message,
    string? SessionId = null,
    Dictionary<string, string>? Filters = null,
    int MaxResults = 5,
    float MinRelevance = 0.3f
);