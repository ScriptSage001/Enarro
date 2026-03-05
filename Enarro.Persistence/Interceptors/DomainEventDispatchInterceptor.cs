using CoreKernel.Primitives.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Enarro.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChangesAsync completes.
/// Collects events from all entities that expose GetDomainEvents/ClearDomainEvents methods
/// (i.e. CoreKernel AggregateRoot{T} subclasses) and publishes them via MediatR.
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
        var domainEvents = new List<IDomainEvent>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            // Look for GetDomainEvents() method (present on CoreKernel AggregateRoot<T>)
            var getEventsMethod = entityType.GetMethod("GetDomainEvents");
            if (getEventsMethod is null) continue;

            if (getEventsMethod.Invoke(entity, null) is IReadOnlyCollection<IDomainEvent> events
                && events.Count > 0)
            {
                domainEvents.AddRange(events);

                var clearEventsMethod = entityType.GetMethod("ClearDomainEvents");
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
