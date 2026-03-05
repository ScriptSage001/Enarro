using Enarro.Domain.Common;

namespace Enarro.Persistence;

/// <summary>
/// Unit of Work implementation wrapping EF Core's SaveChangesAsync.
/// Domain event dispatch is handled by the DomainEventDispatchInterceptor.
/// Audit field population is handled by the AuditableEntityInterceptor.
/// </summary>
public class UnitOfWork(EnarroDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
