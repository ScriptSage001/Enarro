using CoreKernel.Primitives.Abstractions;

namespace Enarro.Domain.Users.Events;

/// <summary>
/// Domain event raised when a new user registers.
/// </summary>
public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    DateTime OccurredOn) : IDomainEvent;
