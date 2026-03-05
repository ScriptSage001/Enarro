namespace Enarro.Application.Models;

public sealed record AuthResultModel(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserModel User);
