using CoreKernel.Messaging.Commands;
using Enarro.Application.Models;

namespace Enarro.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResultModel>;
