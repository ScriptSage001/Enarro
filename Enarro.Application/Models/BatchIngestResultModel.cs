namespace Enarro.Application.Models;

public sealed record BatchIngestResultModel(
    int TotalFiles,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<DocumentIngestResultModel> Results);
