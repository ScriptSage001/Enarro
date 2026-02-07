namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a document tag
/// </summary>
public class DocumentTagEntity
{
    public int Id { get; set; }
    public Guid DocumentId { get; set; }
    public string TagKey { get; set; } = string.Empty;
    public string TagValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public DocumentEntity Document { get; set; } = null!;
}
