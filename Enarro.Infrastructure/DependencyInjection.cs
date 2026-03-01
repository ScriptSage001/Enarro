using Enarro.Application.Abstractions;
using Enarro.Infrastructure.AI;
using Enarro.Infrastructure.Auth;
using Enarro.Infrastructure.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace Enarro.Infrastructure;

/// <summary>
/// Dependency injection registration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Auth
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // AI / Vector Memory
        services.AddSingleton<IVectorMemoryService, KernelMemoryVectorService>();

        // Conversation Store (Redis)
        services.AddSingleton<IConversationStore, RedisConversationStore>();

        return services;
    }
}
