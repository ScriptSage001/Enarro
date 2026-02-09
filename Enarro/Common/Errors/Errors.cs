namespace Enarro.Common.Errors;

/// <summary>
/// Factory methods for creating domain-specific error messages
/// </summary>
public static class Errors
{
    /// <summary>
    /// Authentication-related errors
    /// </summary>
    public static class Auth
    {
        public static string InvalidCredentials() =>
            "Invalid email or password";
        
        public static string UserNotFound(string email) =>
            $"User with email '{email}' not found";
        
        public static string UserInactive() =>
            "User account is inactive";
        
        public static string EmailAlreadyExists(string email) =>
            $"Email '{email}' is already registered";
        
        public static string WeakPassword(string reason) =>
            reason;
        
        public static string InvalidToken() =>
            "Invalid or expired token";
        
        public static string TokenExpired() =>
            "Token has expired";
        
        public static string TokenNotFound() =>
            "Refresh token not found";
    }
    
    /// <summary>
    /// Document-related errors
    /// </summary>
    public static class Documents
    {
        public static string NotFound(string documentId) =>
            $"Document with ID '{documentId}' not found";
        
        public static string UploadFailed(string reason) =>
            $"Document upload failed: {reason}";
        
        public static string DeleteFailed(string documentId, string reason) =>
            $"Failed to delete document '{documentId}': {reason}";
        
        public static string InvalidFormat(string format) =>
            $"Invalid document format: {format}";
        
        public static string TooLarge(long maxSize) =>
            $"Document exceeds maximum size of {maxSize} bytes";
        
        public static string EmptyFile() =>
            "Document file cannot be empty";
    }
    
    /// <summary>
    /// Chat-related errors
    /// </summary>
    public static class Chat
    {
        public static string SessionNotFound(string sessionId) =>
            $"Chat session '{sessionId}' not found";
        
        public static string MessageEmpty() =>
            "Message cannot be empty";
        
        public static string InvalidRelevance() =>
            "MinRelevance must be between 0 and 1";
        
        public static string ProcessingFailed(string reason) =>
            $"Chat processing failed: {reason}";
    }
    
    /// <summary>
    /// Conversation-related errors
    /// </summary>
    public static class Conversation
    {
        public static string SessionNotFound(string sessionId) =>
            $"Conversation session '{sessionId}' not found";
        
        public static string CreationFailed(string reason) =>
            $"Failed to create conversation session: {reason}";
    }
    
    /// <summary>
    /// General errors
    /// </summary>
    public static string ValidationFailed(string message) => message;
    
    public static string UnauthorizedAccess() => "Unauthorized access";
    
    public static string Internal(string message) => message;
    
    public static string NullValue(string parameterName) => $"Value cannot be null: {parameterName}";
}
