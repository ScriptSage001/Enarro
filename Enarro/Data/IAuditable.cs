namespace Enarro.Data;

/// <summary>
/// Marker interface for entities that track audit information.
/// CreatedAt/UpdatedAt are set automatically by EnarroDbContext.SaveChangesAsync.
/// CreatedBy/LastModifiedBy are set from IUserContext.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? LastModifiedBy { get; set; }
}
