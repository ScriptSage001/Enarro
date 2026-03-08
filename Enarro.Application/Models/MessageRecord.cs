namespace Enarro.Application.Models;

public record MessageRecord(
    string SessionId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);