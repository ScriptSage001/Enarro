namespace Enarro.Application.Documents.DTOs;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTime UploadedAt,
    Guid? UserId,
    int ChunkCount,
    string? ErrorMessage,
    IReadOnlyList<DocumentTagDto> Tags);

public record DocumentTagDto(string Key, string Value);

public record DocumentIngestResultDto(
    string DocumentId,
    string FileName,
    string Status,
    string? ErrorMessage);

public record BatchIngestResultDto(
    int TotalFiles,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<DocumentIngestResultDto> Results);
