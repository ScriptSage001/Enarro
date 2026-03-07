using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.DomainMarkers.SoftDeletion;
using Enarro.Application.Abstractions;
using Enarro.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence;

/// <summary>
/// Unit of Work implementation wrapping EF Core's SaveChangesAsync.
/// Domain event dispatch is handled by the DomainEventDispatchInterceptor.
/// Audit field population is handled by the AuditableEntityInterceptor.
/// </summary>
public class UnitOfWork(EnarroDbContext dbContext, ICurrentUserService currentUserService) : IUnitOfWork
{
    private readonly EnarroDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Custom SaveChanges method on top of the SaveChanges of EFCore
    /// Handles Update of Auditable Entities before saving changes to DB
    /// </summary>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        UpdateTimeStampedEntities();
        HandleSoftDelete();
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Sets CreatedBy / LastModifiedBy on IAuditable entities via the ChangeTracker.
    /// </summary>
    private void UpdateAuditableEntities()
    {
        var currentUser = _currentUserService.Email ?? "System";
        var entities = _dbContext.ChangeTracker.Entries<IAuditable>();

        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Added)
            {
                entity
                    .Property(x => x.CreatedBy)
                    .CurrentValue = currentUser;
            }

            if (entity.State is EntityState.Added or EntityState.Modified)
            {
                entity
                    .Property(x => x.LastModifiedBy)
                    .CurrentValue = currentUser;
            }
        }
    }

    /// <summary>
    /// Sets CreatedOn / LastModifiedOn on ITimeStamped entities via the ChangeTracker.
    /// </summary>
    private void UpdateTimeStampedEntities()
    {
        var now = DateTimeOffset.UtcNow;
        var entities = _dbContext.ChangeTracker.Entries<ITimeStamped>();

        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Added)
            {
                entity
                    .Property(x => x.CreatedOn)
                    .CurrentValue = now;
            }

            if (entity.State is EntityState.Added or EntityState.Modified)
            {
                entity
                    .Property(x => x.LastModifiedOn)
                    .CurrentValue = now;
            }
        }
    }

    /// <summary>
    /// To set SoftDelete Property of the SoftDeletable Entities
    /// </summary>
    private void HandleSoftDelete()
    {
        var currentUser = _currentUserService.UserId.ToString();
        var entities = _dbContext.ChangeTracker.Entries<ISoftDeletable>();

        foreach (var entity in entities)
        {
            switch (entity.State)
            {
                case EntityState.Deleted:
                    entity.State = EntityState.Modified;

                    entity
                        .Property(x => x.IsDeleted)
                        .CurrentValue = true;

                    entity
                        .Property(x => x.DeletedOn)
                        .CurrentValue = DateTimeOffset.Now;

                    entity
                        .Property(x => x.DeletedBy)
                        .CurrentValue = currentUser!;
                    break;

                case EntityState.Added:
                    entity
                        .Property(x => x.IsDeleted)
                        .CurrentValue = false;
                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Modified:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    #endregion
}
