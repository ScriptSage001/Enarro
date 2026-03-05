using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enarro.Persistence;

/// <summary>
/// Design-time factory for EnarroDbContext, used by EF Core tooling (dotnet ef migrations).
/// Provides a dummy connection string for migration generation only.
/// At runtime, the real connection string comes from Aspire's AddNpgsqlDbContext.
/// </summary>
public class EnarroDbContextFactory : IDesignTimeDbContextFactory<EnarroDbContext>
{
    public EnarroDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnarroDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=enarro_design;Username=postgres;Password=postgres");

        return new EnarroDbContext(optionsBuilder.Options);
    }
}
