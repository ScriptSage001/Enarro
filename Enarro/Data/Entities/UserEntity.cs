namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a user in the system
/// </summary>
public class UserEntity : IAuditable
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Admin, User, Guest
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Navigation properties
    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = new List<RefreshTokenEntity>();
    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
}
