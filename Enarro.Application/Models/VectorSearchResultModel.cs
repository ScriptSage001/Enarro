namespace Enarro.Application.Models;

/// <summary>
/// Result of a vector search / RAG query.
/// </summary>
public record VectorSearchResultModel(
    string Answer,
    IReadOnlyList<VectorCitationModel> Citations,
    bool IsAnswerRelevant);
