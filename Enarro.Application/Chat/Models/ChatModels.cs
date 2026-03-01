namespace Enarro.Application.Chat.Models;

public sealed record ChatResultModel(
    string Answer,
    IReadOnlyList<CitationModel> Citations,
    double ConfidenceScore,
    string SessionId,
    int TokensUsed);

public sealed record CitationModel(
    string DocumentId,
    string FileName,
    string Excerpt,
    double Relevance);
