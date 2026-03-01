using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Enarro.Domain.Users;
using Enarro.Persistence.Interceptors;
using Enarro.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enarro.Persistence;

/// <summary>
/// Dependency injection registration for the Persistence layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // Interceptors
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventDispatchInterceptor>();

        // DbContext is registered via Aspire's AddNpgsqlDbContext in the API project
        // This method registers the interceptors and repositories

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
