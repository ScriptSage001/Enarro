namespace Enarro.Models.Document;

/// <summary>
/// Result of a batch document ingestion operation
/// </summary>
public record BatchIngestResult(
    int TotalFiles,
    int SuccessCount,
    int FailedCount,
    List<DocumentIngestResult> Results
);

/// <summary>
/// Result of a single document ingestion
/// </summary>
public record DocumentIngestResult(
    string? DocumentId,
    string FileName,
    bool Success,
    string? ErrorMessage
);
