using CoreKernel.DomainMarkers.Auditing;
using CoreKernel.DomainMarkers.SoftDeletion;
using Enarro.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Enarro.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that auto-populates IAuditable and ISoftDeletable fields before saving.
/// </summary>
public class AuditableEntityInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var userId = currentUserService.UserId?.ToString() ?? "system";

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedOn = now;
                    auditable.CreatedBy = userId;
                }

                auditable.LastModifiedOn = now;
                auditable.LastModifiedBy = userId;
            }

            // Convert hard deletes to soft deletes for ISoftDeletable
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedOn = now;
                softDeletable.DeletedBy = userId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
