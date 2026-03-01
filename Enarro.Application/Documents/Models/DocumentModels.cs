namespace Enarro.Application.Documents.Models;

public sealed record DocumentModel(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTime UploadedAt,
    Guid? UserId,
    int ChunkCount,
    string? ErrorMessage,
    IReadOnlyList<DocumentTagModel> Tags);

public sealed record DocumentTagModel(string Key, string Value);

public sealed record DocumentIngestResultModel(
    string DocumentId,
    string FileName,
    string Status,
    string? ErrorMessage);

public sealed record BatchIngestResultModel(
    int TotalFiles,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<DocumentIngestResultModel> Results);
