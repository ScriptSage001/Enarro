using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Enarro.Data;

/// <summary>
/// Generic repository interface for entity data access.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    Task<T?> GetByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Finds the first entity matching a predicate, with optional eager loading.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an IQueryable for building complex queries, with optional eager loading.
    /// </summary>
    IQueryable<T> Query(Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);

    /// <summary>
    /// Adds an entity to the context.
    /// </summary>
    void Add(T entity);

    /// <summary>
    /// Removes an entity from the context.
    /// </summary>
    void Remove(T entity);
}
