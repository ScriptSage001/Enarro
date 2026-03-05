namespace Enarro.Application.Models;

public sealed record DocumentIngestResultModel(
    string DocumentId,
    string FileName,
    string Status,
    string? ErrorMessage);
