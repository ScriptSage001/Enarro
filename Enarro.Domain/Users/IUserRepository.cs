using Enarro.Domain.Common;

namespace Enarro.Domain.Users;

/// <summary>
/// Repository contract for the User aggregate.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user with their refresh tokens eagerly loaded.
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    void Add(User user);
}
