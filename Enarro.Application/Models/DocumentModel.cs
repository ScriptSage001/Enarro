namespace Enarro.Application.Models;

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
