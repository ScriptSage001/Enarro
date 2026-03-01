using System.Text.RegularExpressions;
using CoreKernel.Primitives.ValueObjects;

namespace Enarro.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated, immutable email address.
/// </summary>
public sealed partial class Email : ValueObject
{
    private static readonly Regex EmailRegex = MyRegex();

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an Email value object after validation.
    /// </summary>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid email format: '{email}'.", nameof(email));

        return new Email(normalized);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
