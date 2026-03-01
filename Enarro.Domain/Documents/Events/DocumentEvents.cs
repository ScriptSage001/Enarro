using CoreKernel.Primitives.Abstractions;

namespace Enarro.Domain.Documents.Events;

/// <summary>
/// Domain event raised when a document has been successfully ingested and indexed.
/// </summary>
public sealed record DocumentIngestedEvent(
    Guid DocumentId,
    string FileName,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Domain event raised when a document has been deleted.
/// </summary>
public sealed record DocumentDeletedEvent(
    Guid DocumentId,
    string FileName,
    DateTime OccurredOn) : IDomainEvent;
