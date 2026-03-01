using CoreKernel.Messaging.Commands;
using Enarro.Application.Auth.DTOs;

namespace Enarro.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : ICommand<AuthResult>;
