using Enarro.Application.Abstractions;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Enarro.Domain.Users;
using Enarro.Persistence.Interceptors;
using Enarro.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Enarro.Persistence;

/// <summary>
/// Dependency injection registration for the Persistence layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Persistence services: DbContext (via Aspire), interceptors, repositories, and health checks.
    /// </summary>
    public static IHostApplicationBuilder AddPersistence(this IHostApplicationBuilder builder)
    {
        // Aspire-managed PostgreSQL + EF Core DbContext
        builder.AddNpgsqlDbContext<EnarroDbContext>("enarro-db");

        // Interceptors (Scoped — they depend on scoped services like ICurrentUserService, IPublisher)
        builder.Services.AddScoped<AuditableEntityInterceptor>();
        builder.Services.AddScoped<DomainEventDispatchInterceptor>();

        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Unit of Work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Query Services
        builder.Services.AddScoped<IDocumentQueryService, QueryServices.DocumentQueryService>();

        // Conversation
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

        // Health Checks
        var connectionString = builder.Configuration.GetConnectionString("enarro-db");
        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.Services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgresql", tags: ["db", "sql"]);
        }

        return builder;
    }
}
