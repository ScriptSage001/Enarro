namespace Enarro.Common.Errors;

/// <summary>
/// Error code constants for the application
/// </summary>
public static class ErrorCodes
{
    // Authentication Errors
    public const string AuthInvalidCredentials = "Auth.InvalidCredentials";
    public const string AuthUserNotFound = "Auth.UserNotFound";
    public const string AuthUserInactive = "Auth.UserInactive";
    public const string AuthEmailAlreadyExists = "Auth.EmailAlreadyExists";
    public const string AuthWeakPassword = "Auth.WeakPassword";
    public const string AuthInvalidToken = "Auth.InvalidToken";
    public const string AuthTokenExpired = "Auth.TokenExpired";
    public const string AuthTokenNotFound = "Auth.TokenNotFound";
    
    // Document Errors
    public const string DocumentNotFound = "Document.NotFound";
    public const string DocumentUploadFailed = "Document.UploadFailed";
    public const string DocumentDeleteFailed = "Document.DeleteFailed";
    public const string DocumentInvalidFormat = "Document.InvalidFormat";
    public const string DocumentTooLarge = "Document.TooLarge";
    public const string DocumentEmptyFile = "Document.EmptyFile";
    
    // Chat Errors
    public const string ChatSessionNotFound = "Chat.SessionNotFound";
    public const string ChatMessageEmpty = "Chat.MessageEmpty";
    public const string ChatInvalidRelevance = "Chat.InvalidRelevance";
    public const string ChatProcessingFailed = "Chat.ProcessingFailed";
    
    // Conversation Errors
    public const string ConversationSessionNotFound = "Conversation.SessionNotFound";
    public const string ConversationCreationFailed = "Conversation.CreationFailed";
    
    // General Errors
    public const string ValidationFailed = "Validation.Failed";
    public const string UnauthorizedAccess = "Unauthorized.Access";
    public const string InternalError = "Internal.Error";
    public const string NullValue = "Null.Value";
}
