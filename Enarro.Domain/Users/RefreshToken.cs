using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.Primitives.Entities;

namespace Enarro.Domain.Users;

/// <summary>
/// Entity representing a refresh token for JWT authentication.
/// Owned by User aggregate.
/// </summary>
public class RefreshToken : Entity<Guid>, ITimeStamped
{
    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private RefreshToken() : base() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string token,
        DateTime expiresAt) : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    #region ITimeStamped

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastModifiedOn { get; set; }

    #endregion

    #region Factory

    public static RefreshToken Create(Guid userId, string token, int expirationDays)
    {
        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            token,
            DateTime.UtcNow.AddDays(expirationDays));
    }

    #endregion

    #region Domain Behavior

    public bool IsExpired() => ExpiresAt < DateTime.UtcNow;

    public bool IsValid() => !IsRevoked && !IsExpired();

    public void Revoke(string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason ?? "Manually revoked";
    }

    #endregion
}
