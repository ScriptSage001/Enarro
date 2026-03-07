namespace Enarro.Contracts.Chat;

/// <summary>
/// Represents a source citation for a chat response
/// </summary>
public record Citation(
    string DocumentId,
    string DocumentName,
    string Excerpt,
    double Relevance,
    int PageNumber = 0);