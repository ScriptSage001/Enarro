namespace Enarro.Application.Abstractions;

/// <summary>
/// Result of a vector search / RAG query.
/// </summary>
public record VectorSearchResult(
    string Answer,
    IReadOnlyList<VectorCitation> Citations,
    bool IsAnswerRelevant);
