using CoreKernel.Primitives.ValueObjects;

namespace Enarro.Domain.Common;

/// <summary>
/// Strongly-typed identifier for Document aggregate.
/// </summary>
public sealed class DocumentId : StronglyTypedId<Guid>
{
    public DocumentId(Guid value) : base(value) { }

    public static DocumentId New() => new(Guid.NewGuid());
    
    public static DocumentId From(Guid value) => new(value);
}
