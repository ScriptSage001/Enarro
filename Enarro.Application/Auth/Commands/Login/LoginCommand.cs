using CoreKernel.Messaging.Commands;
using Enarro.Application.Auth.DTOs;

namespace Enarro.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthResult>;
