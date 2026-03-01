namespace Enarro.Application.Models;

/// <summary>
/// A citation from the vector search.
/// </summary>
public record VectorCitationModel(
    string DocumentId,
    string FileName,
    string Excerpt,
    double Relevance);
