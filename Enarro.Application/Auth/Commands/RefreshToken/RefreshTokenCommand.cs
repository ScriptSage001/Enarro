using CoreKernel.Messaging.Commands;
using Enarro.Application.Auth.DTOs;

namespace Enarro.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResult>;
