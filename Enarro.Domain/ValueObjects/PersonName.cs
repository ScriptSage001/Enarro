using CoreKernel.Primitives.ValueObjects;

namespace Enarro.Domain.ValueObjects;

/// <summary>
/// Value object representing a person's name (first + last).
/// </summary>
public sealed class PersonName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static PersonName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        return new PersonName(firstName.Trim(), lastName.Trim());
    }

    public string FullName => $"{FirstName} {LastName}";

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => FullName;
}
