using Enarro.Domain.Common;
using Enarro.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence.Repositories;

public class UserRepository(EnarroDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => EF.Property<string>(u.Email, "Value") == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.Users
            .AnyAsync(u => EF.Property<string>(u.Email, "Value") == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken), cancellationToken);
    }

    public void Add(User user) => dbContext.Users.Add(user);
}
