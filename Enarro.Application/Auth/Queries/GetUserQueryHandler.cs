using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Queries;

public sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, UserModel>
{
    public async Task<Result<UserModel>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(UserId.From(query.UserId), cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserModel>(UserErrors.UserNotFound(query.UserId.ToString()));
        }

        return new UserModel(
            user.Id.Value,
            user.Email.Value,
            user.Name.FirstName,
            user.Name.LastName,
            user.Role,
            user.IsActive,
            user.LastLoginAt);
    }
}
