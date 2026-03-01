using CoreKernel.Primitives.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Enarro.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChangesAsync completes.
/// Collects events from all AggregateRoots and publishes them via MediatR's IPublisher.
/// </summary>
public class DomainEventDispatchInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return result;
    }

    private async Task DispatchDomainEventsAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {
        // Find all aggregate roots with pending domain events
        var aggregateRoots = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRootMarker)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = new List<CoreKernel.Primitives.Abstractions.IDomainEvent>();

        foreach (var entity in aggregateRoots)
        {
            // Use reflection to get domain events since the base type is generic
            var getEventsMethod = entity.GetType().GetMethod("GetDomainEvents");
            var clearEventsMethod = entity.GetType().GetMethod("ClearDomainEvents");

            if (getEventsMethod?.Invoke(entity, null) is IReadOnlyCollection<CoreKernel.Primitives.Abstractions.IDomainEvent> events
                && events.Count > 0)
            {
                domainEvents.AddRange(events);
                clearEventsMethod?.Invoke(entity, null);
            }
        }

        // Publish all domain events via MediatR
        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

/// <summary>
/// Internal marker to identify aggregate roots without knowing the generic type.
/// </summary>
internal interface IAggregateRootMarker { }
