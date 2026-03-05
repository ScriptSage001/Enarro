namespace Enarro.Application.Models;

public sealed record ChatResultModel(
    string Answer,
    IReadOnlyList<CitationModel> Citations,
    double ConfidenceScore,
    string SessionId,
    int TokensUsed);
