namespace Enarro.Application.Abstractions;

/// <summary>
/// A citation from the vector search.
/// </summary>
public record VectorCitation(
    string DocumentId,
    string FileName,
    string Excerpt,
    double Relevance);
