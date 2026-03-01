using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Commands;

public sealed class RevokeTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RevokeTokenCommand>
{
    public async Task<Result> Handle(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshTokenAsync(command.RefreshToken, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.TokenNotFound());
        }

        user.RevokeRefreshToken(command.RefreshToken, command.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
