using CoreKernel.Primitives.ValueObjects;

namespace Enarro.Domain.Documents;

/// <summary>
/// Value object representing a document tag (key-value pair).
/// Immutable — tags are compared by their key and value.
/// </summary>
public sealed class DocumentTag : ValueObject
{
    public string TagKey { get; }
    public string TagValue { get; }

    private DocumentTag(string tagKey, string tagValue)
    {
        TagKey = tagKey;
        TagValue = tagValue;
    }

    public static DocumentTag Create(string tagKey, string tagValue)
    {
        if (string.IsNullOrWhiteSpace(tagKey))
            throw new ArgumentException("Tag key cannot be empty.", nameof(tagKey));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value cannot be empty.", nameof(tagValue));

        return new DocumentTag(tagKey.Trim(), tagValue.Trim());
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return TagKey;
        yield return TagValue;
    }

    public override string ToString() => $"{TagKey}={TagValue}";
}
