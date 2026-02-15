using CoreKernel.Functional.Results;

namespace Enarro.Common.Errors;

/// <summary>
/// Factory methods for creating domain-specific Error records
/// </summary>
public static class Errors
{
    /// <summary>
    /// Authentication-related errors
    /// </summary>
    public static class Auth
    {
        public static Error InvalidCredentials() =>
            Unauthorized(ErrorCodes.AuthInvalidCredentials, "Invalid email or password");

        public static Error UserNotFound(string email) =>
            NotFound(ErrorCodes.AuthUserNotFound, $"User with email '{email}' not found");

        public static Error UserInactive() =>
            Unauthorized(ErrorCodes.AuthUserInactive, "User account is inactive");

        public static Error EmailAlreadyExists(string email) =>
            Conflict(ErrorCodes.AuthEmailAlreadyExists, $"Email '{email}' is already registered");

        public static Error WeakPassword(string reason) =>
            Error.Validation(ErrorCodes.AuthWeakPassword, reason);

        public static Error InvalidToken() =>
            Unauthorized(ErrorCodes.AuthInvalidToken, "Invalid or expired token");

        public static Error TokenExpired() =>
            Unauthorized(ErrorCodes.AuthTokenExpired, "Token has expired");

        public static Error TokenNotFound() =>
            NotFound(ErrorCodes.AuthTokenNotFound, "Refresh token not found");
    }

    /// <summary>
    /// Document-related errors
    /// </summary>
    public static class Documents
    {
        public static Error NotFound(string documentId) =>
            Errors.NotFound(ErrorCodes.DocumentNotFound, $"Document with ID '{documentId}' not found");

        public static Error UploadFailed(string reason) =>
            Error.Failure(ErrorCodes.DocumentUploadFailed, $"Document upload failed: {reason}");

        public static Error DeleteFailed(string documentId, string reason) =>
            Error.Failure(ErrorCodes.DocumentDeleteFailed, $"Failed to delete document '{documentId}': {reason}");

        public static Error InvalidFormat(string format) =>
            Error.Validation(ErrorCodes.DocumentInvalidFormat, $"Invalid document format: {format}");

        public static Error TooLarge(long maxSize) =>
            Error.Validation(ErrorCodes.DocumentTooLarge, $"Document exceeds maximum size of {maxSize} bytes");

        public static Error EmptyFile() =>
            Error.Validation(ErrorCodes.DocumentEmptyFile, "Document file cannot be empty");
    }

    /// <summary>
    /// Chat-related errors
    /// </summary>
    public static class Chat
    {
        public static Error SessionNotFound(string sessionId) =>
            NotFound(ErrorCodes.ChatSessionNotFound, $"Chat session '{sessionId}' not found");

        public static Error MessageEmpty() =>
            Error.Validation(ErrorCodes.ChatMessageEmpty, "Message cannot be empty");

        public static Error InvalidRelevance() =>
            Error.Validation(ErrorCodes.ChatInvalidRelevance, "MinRelevance must be between 0 and 1");

        public static Error ProcessingFailed(string reason) =>
            Error.Failure(ErrorCodes.ChatProcessingFailed, $"Chat processing failed: {reason}");
    }

    /// <summary>
    /// Conversation-related errors
    /// </summary>
    public static class Conversation
    {
        public static Error SessionNotFound(string sessionId) =>
            NotFound(ErrorCodes.ConversationSessionNotFound, $"Conversation session '{sessionId}' not found");

        public static Error CreationFailed(string reason) =>
            Error.Failure(ErrorCodes.ConversationCreationFailed, $"Failed to create conversation session: {reason}");
    }

    /// <summary>
    /// General errors
    /// </summary>
    public static Error ValidationFailed(string message) =>
        Error.Validation(ErrorCodes.ValidationFailed, message);

    public static Error UnauthorizedAccess() =>
        Unauthorized(ErrorCodes.UnauthorizedAccess, "Unauthorized access");

    public static Error Internal(string message) =>
        Error.Failure(ErrorCodes.InternalError, message);

    public static Error NullValue(string parameterName) =>
        Error.Failure(ErrorCodes.NullValue, $"Value cannot be null: {parameterName}");

    /// <summary>
    /// Creates an Unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message)
    {
        ValidateInput(code, message);
        return new Error(code, message, ErrorType.Unauthorized);
    }

    /// <summary>
    /// Creates a NotFound error.
    /// </summary>
    public static Error NotFound(string code, string message)
    {
        ValidateInput(code, message);
        return new Error(code, message, ErrorType.NotFound);
    }

    /// <summary>
    /// Creates a Conflict error.
    /// </summary>
    public static Error Conflict(string code, string message)
    {
        ValidateInput(code, message);
        return new Error(code, message, ErrorType.Conflict);
    }

    /// <summary>
    /// Validates the input parameters for error creation.
    /// </summary>
    private static void ValidateInput(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or empty.", nameof(message));
    }
}
