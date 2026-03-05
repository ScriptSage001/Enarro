using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.DomainMarkers.SoftDeletion;
using CoreKernel.Primitives.Entities;
using Enarro.Domain.Common;
using Enarro.Domain.Documents.Events;

namespace Enarro.Domain.Documents;

/// <summary>
/// Aggregate Root representing a document in the system.
/// Encapsulates document lifecycle (upload → processing → indexed/failed → deleted).
/// </summary>
public class Document : AggregateRoot<DocumentId>, IAuditable, ISoftDeletable
{
    private readonly List<DocumentTag> _tags = [];

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private Document() : base() { }

    private Document(
        DocumentId id,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid? userId) : base(id)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = DateTime.UtcNow;
        UserId = userId;
        Status = DocumentStatus.Uploading;
    }

    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid? UserId { get; private set; }
    public DocumentStatus Status { get; private set; }
    public int ChunkCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation: owned collection of tags
    public IReadOnlyCollection<DocumentTag> Tags => _tags.AsReadOnly();

    #region IAuditable

    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedOn { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;

    #endregion

    #region ISoftDeletable

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    #region Factory

    /// <summary>
    /// Creates a new document record for an incoming file upload.
    /// </summary>
    public static Document Create(
        string fileName,
        string contentType,
        long sizeBytes,
        Guid? userId,
        IEnumerable<KeyValuePair<string, string>>? tags = null)
    {
        var document = new Document(
            DocumentId.New(),
            fileName,
            contentType,
            sizeBytes,
            userId);

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                document._tags.Add(DocumentTag.Create(tag.Key, tag.Value));
            }
        }

        return document;
    }

    #endregion

    #region Domain Behavior

    public void MarkAsProcessing()
    {
        Status = DocumentStatus.Processing;
    }

    public void MarkAsIndexed(int chunkCount = 0)
    {
        Status = DocumentStatus.Indexed;
        ChunkCount = chunkCount;

        RaiseDomainEvent(new DocumentIngestedEvent(
            Id.Value, FileName, DateTime.UtcNow));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void AddTag(string key, string value)
    {
        var tag = DocumentTag.Create(key, value);
        if (!_tags.Contains(tag))
        {
            _tags.Add(tag);
        }
    }

    public void MarkAsDeleted()
    {
        Status = DocumentStatus.Deleted;

        RaiseDomainEvent(new DocumentDeletedEvent(
            Id.Value, FileName, DateTime.UtcNow));
    }

    #endregion
}
