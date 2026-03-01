using CoreKernel.Messaging.Commands;
using Enarro.Application.Auth.Models;

namespace Enarro.Application.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthResult>;
