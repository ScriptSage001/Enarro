using Enarro.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Enarro.Application;

/// <summary>
/// Dependency injection registration for the Application layer.
/// Registers MediatR with assembly scanning for all CoreKernel.Messaging handler interfaces.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR — scans this assembly for ICommandHandler, IQueryHandler, IDomainEventHandler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Application services
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
