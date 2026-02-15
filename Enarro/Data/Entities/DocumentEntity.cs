namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a document in the system
/// </summary>
public class DocumentEntity : IAuditable
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public string Status { get; set; } = "Uploading";
    public int ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Foreign key to User
    public Guid? UserId { get; set; }
    
    // Navigation properties
    public UserEntity? User { get; set; }
    public ICollection<DocumentTagEntity> Tags { get; set; } = new List<DocumentTagEntity>();
}
