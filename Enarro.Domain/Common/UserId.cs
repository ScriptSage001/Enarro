using CoreKernel.Primitives.ValueObjects;

namespace Enarro.Domain.Common;

/// <summary>
/// Strongly-typed identifier for User aggregate.
/// </summary>
public sealed class UserId : StronglyTypedId<Guid>
{
    public UserId(Guid value) : base(value) { }

    public static UserId New() => new(Guid.NewGuid());
    
    public static UserId From(Guid value) => new(value);
}
