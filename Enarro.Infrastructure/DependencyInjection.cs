using Enarro.Application.Abstractions;
using Enarro.Infrastructure.AI;
using Enarro.Infrastructure.Auth;
using Enarro.Infrastructure.Cache;
using Enarro.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enarro.Infrastructure;

/// <summary>
/// Dependency injection registration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // AI / Vector Memory
        services.AddSingleton<IVectorMemoryService, KernelMemoryVectorService>();

        // Conversation Store (Redis)
        services.AddSingleton<IConversationStore, RedisConversationStore>();

        // HttpClientFactory (for health checks and future HTTP-based services)
        services.AddHttpClient();

        // Health Checks
        var redisConnectionString = configuration.GetConnectionString("redis");
        var healthChecks = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, name: "redis", tags: ["cache"]);
        }

        healthChecks
            .AddCheck<OllamaHealthCheck>("ollama", tags: ["llm"])
            .AddCheck<QdrantHealthCheck>("qdrant", tags: ["vector-db"]);

        return services;
    }
}
