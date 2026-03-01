using Enarro.Domain.Documents;
using Enarro.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence;

/// <summary>
/// EF Core DbContext for the Enarro application.
/// Maps rich domain entities to the database.
/// </summary>
public class EnarroDbContext : DbContext
{
    public EnarroDbContext(DbContextOptions<EnarroDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnarroDbContext).Assembly);
    }
}
