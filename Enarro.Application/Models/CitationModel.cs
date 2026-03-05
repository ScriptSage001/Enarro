namespace Enarro.Application.Models;

public sealed record CitationModel(
    string DocumentId,
    string FileName,
    string Excerpt,
    double Relevance);
