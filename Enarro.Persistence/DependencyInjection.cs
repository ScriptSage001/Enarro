using Enarro.Application.Abstractions;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Enarro.Domain.Users;
using Enarro.Persistence.Interceptors;
using Enarro.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enarro.Persistence;

/// <summary>
/// Dependency injection registration for the Persistence layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        // DbContext is registered via Aspire's AddNpgsqlDbContext in the API project
        // This method registers the interceptors and repositories

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Query Services
        services.AddScoped<IDocumentQueryService, QueryServices.DocumentQueryService>();

        // Health Checks
        var connectionString = configuration.GetConnectionString("enarro-db");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgresql", tags: ["db", "sql"]);
        }

        return services;
    }
}
