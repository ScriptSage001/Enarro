using CoreKernel.Messaging.Commands;

namespace Enarro.Application.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken, string? Reason) : ICommand;
