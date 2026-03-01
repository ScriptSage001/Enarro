using CoreKernel.Messaging.Queries;
using Enarro.Application.Auth.DTOs;

namespace Enarro.Application.Auth.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
