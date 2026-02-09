namespace Enarro.Data.Entities;

/// <summary>
/// Entity representing a refresh token for JWT authentication
/// </summary>
public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Navigation property
    public UserEntity User { get; set; } = null!;
}
