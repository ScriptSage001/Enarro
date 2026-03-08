using Enarro.Domain.Common;

namespace Enarro.Application.Models;

public record SessionRecord(
    string SessionId,
    UserId UserId,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);