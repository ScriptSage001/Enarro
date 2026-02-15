using Enarro.Data.Entities;

namespace Enarro.Data;

/// <summary>
/// Unit of Work interface — coordinates repositories and persists changes with audit tracking.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<UserEntity> Users { get; }
    IRepository<DocumentEntity> Documents { get; }
    IRepository<DocumentTagEntity> DocumentTags { get; }
    IRepository<RefreshTokenEntity> RefreshTokens { get; }

    /// <summary>
    /// Applies audit information to IAuditable entities and saves all changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
