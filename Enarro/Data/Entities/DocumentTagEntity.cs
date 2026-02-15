namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a document tag
/// </summary>
public class DocumentTagEntity : IAuditable
{
    public int Id { get; set; }
    public Guid DocumentId { get; set; }
    public string TagKey { get; set; } = string.Empty;
    public string TagValue { get; set; } = string.Empty;
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    public DocumentEntity Document { get; set; } = null!;
}
