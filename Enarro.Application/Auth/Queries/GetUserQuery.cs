using CoreKernel.Messaging.Queries;
using Enarro.Application.Auth.Models;

namespace Enarro.Application.Auth.Queries;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserModel>;
