namespace Enarro.Models.Session;

/// <summary>
/// Information about a user session
/// </summary>
public record SessionInfo(
    string SessionId,
    string? UserId,
    DateTime CreatedAt,
    DateTime? LastAccessedAt = null,
    Dictionary<string, object>? Metadata = null
);
