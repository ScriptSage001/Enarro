using CoreKernel.Messaging.Commands;
using Enarro.Application.Models;

namespace Enarro.Application.Auth.Commands;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : ICommand<AuthResultModel>;
