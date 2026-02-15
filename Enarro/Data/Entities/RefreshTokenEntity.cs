namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a refresh token for JWT authentication
/// </summary>
public class RefreshTokenEntity : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Navigation property
    public UserEntity User { get; set; } = null!;
}
