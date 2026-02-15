using Microsoft.EntityFrameworkCore;
using Enarro.Common;
using Enarro.Data.Entities;

namespace Enarro.Data;

/// <summary>
/// Scoped Unit of Work that coordinates repositories and applies audit info on save.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly EnarroDbContext _context;
    private readonly IUserContext _userContext;

    private IRepository<UserEntity>? _users;
    private IRepository<DocumentEntity>? _documents;
    private IRepository<DocumentTagEntity>? _documentTags;
    private IRepository<RefreshTokenEntity>? _refreshTokens;

    public UnitOfWork(EnarroDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public IRepository<UserEntity> Users => _users ??= new Repository<UserEntity>(_context);
    public IRepository<DocumentEntity> Documents => _documents ??= new Repository<DocumentEntity>(_context);
    public IRepository<DocumentTagEntity> DocumentTags => _documentTags ??= new Repository<DocumentTagEntity>(_context);
    public IRepository<RefreshTokenEntity> RefreshTokens => _refreshTokens ??= new Repository<RefreshTokenEntity>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;
        var userId = _userContext.UserId?.ToString();

        foreach (var entry in _context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.LastModifiedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.LastModifiedBy = userId;
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }
}
