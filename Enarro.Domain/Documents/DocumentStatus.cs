namespace Enarro.Domain.Documents;

/// <summary>
/// Document processing status.
/// </summary>
public enum DocumentStatus
{
    Uploading,
    Processing,
    Indexed,
    Failed,
    Deleted
}
