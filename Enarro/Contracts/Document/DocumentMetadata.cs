namespace Enarro.Contracts.Document;

/// <summary>
/// Represents metadata for an uploaded document
/// </summary>
public record DocumentMetadata(
    string Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt,
    string? UploadedBy,
    DocumentStatus Status,
    Dictionary<string, string> Tags,
    int ChunkCount = 0,
    string? ErrorMessage = null
);

/// <summary>
/// Document processing status
/// </summary>
public enum DocumentStatus
{
    Uploading,
    Processing,
    Indexed,
    Failed,
    Deleted
}
