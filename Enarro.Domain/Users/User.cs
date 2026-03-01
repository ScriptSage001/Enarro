using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.DomainMarkers.SoftDeletion;
using CoreKernel.Primitives.Entities;
using Enarro.Domain.Common;
using Enarro.Domain.Users.Events;
using Enarro.Domain.ValueObjects;

namespace Enarro.Domain.Users;

/// <summary>
/// Aggregate Root representing a user in the system.
/// Owns refresh tokens. Encapsulates registration, login, and account management.
/// </summary>
public class User : AggregateRoot<UserId>, IAuditable, ISoftDeletable
{
    private readonly List<RefreshToken> _refreshTokens = [];

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private User() : base() { }

    private User(
        UserId id,
        Email email,
        string passwordHash,
        PersonName name,
        string role = "User") : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Name = name;
        Role = role;
        IsActive = true;
    }

    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public PersonName Name { get; private set; } = null!;
    public string Role { get; private set; } = "User";
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    // Navigation: owned collection of refresh tokens
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

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
    /// Creates a new user with hashed password and raises a registration event.
    /// </summary>
    public static User Register(
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        var userId = UserId.New();
        var user = new User(
            userId,
            Email.Create(email),
            passwordHash,
            PersonName.Create(firstName, lastName));

        user.RaiseDomainEvent(new UserRegisteredEvent(
            userId.Value, email, DateTime.UtcNow));

        return user;
    }

    #endregion

    #region Domain Behavior

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public RefreshToken AddRefreshToken(string token, int expirationDays)
    {
        var refreshToken = RefreshToken.Create(Id.Value, token, expirationDays);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(string token, string? reason = null)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
        refreshToken?.Revoke(reason);
    }

    #endregion
}
