namespace Enarro.Models.Chat;

/// <summary>
/// Represents a source citation for a chat response
/// </summary>
public record Citation(
    string DocumentId,
    string DocumentName,
    string Excerpt,
    float Relevance,
    int PageNumber = 0
);
