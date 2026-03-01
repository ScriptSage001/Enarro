using CoreKernel.Functional.Results;

namespace Enarro.Domain.Documents;

/// <summary>
/// Domain-specific error factory for the Document aggregate.
/// </summary>
public static class DocumentErrors
{
    public static Error NotFound(string documentId) =>
        new("Document.NotFound", $"Document with ID '{documentId}' not found", ErrorType.NotFound);

    public static Error EmptyFile() =>
        Error.Validation("Document.EmptyFile", "Document file cannot be empty");

    public static Error UploadFailed(string reason) =>
        Error.Failure("Document.UploadFailed", $"Document upload failed: {reason}");

    public static Error DeleteFailed(string documentId, string reason) =>
        Error.Failure("Document.DeleteFailed", $"Failed to delete document '{documentId}': {reason}");

    public static Error InvalidFormat(string format) =>
        Error.Validation("Document.InvalidFormat", $"Invalid document format: {format}");

    public static Error TooLarge(long maxSize) =>
        Error.Validation("Document.TooLarge", $"Document exceeds maximum size of {maxSize} bytes");
}
