using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Enarro.Data;

/// <summary>
/// Generic repository implementation backed by EF Core DbSet.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default) where TKey : notnull
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public IQueryable<T> Query(Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet;

        if (include != null)
            query = include(query);

        return query;
    }

    public void Add(T entity) => _dbSet.Add(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);
}
