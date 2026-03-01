namespace Enarro.Domain.Common;

/// <summary>
/// Unit of Work contract — persists all changes within a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes and dispatches domain events.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
