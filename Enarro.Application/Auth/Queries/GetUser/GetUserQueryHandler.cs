using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Auth.DTOs;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Queries.GetUser;

public sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(UserId.From(query.UserId), cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>(UserErrors.UserNotFound(query.UserId.ToString()));
        }

        return Result.Success(new UserDto(
            user.Id.Value,
            user.Email.Value,
            user.Name.FirstName,
            user.Name.LastName,
            user.Role,
            user.IsActive,
            user.LastLoginAt));
    }
}
