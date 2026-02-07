using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enarro.Data;

/// <summary>
/// Design-time factory for EnarroDbContext to enable EF Core migrations
/// </summary>
public class EnarroDbContextFactory : IDesignTimeDbContextFactory<EnarroDbContext>
{
    public EnarroDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnarroDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden at runtime by Aspire
        optionsBuilder.UseNpgsql("Host=localhost;Database=enarro_dev;Username=postgres;Password=postgres");
        
        return new EnarroDbContext(optionsBuilder.Options);
    }
}
