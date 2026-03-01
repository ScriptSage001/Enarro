using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Commands;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<LoginCommand, AuthResultModel>
{
    public async Task<Result<AuthResultModel>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResultModel>(UserErrors.InvalidCredentials());
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResultModel>(UserErrors.UserInactive());
        }

        if (!BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResultModel>(UserErrors.InvalidCredentials());
        }

        user.UpdateLastLogin();

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id.Value, user.Email.Value, user.Role);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshTokenValue, jwtTokenService.RefreshTokenExpirationDays);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtTokenService.AccessTokenExpirationMinutes);

        var userModel = new UserModel(
            user.Id.Value,
            user.Email.Value,
            user.Name.FirstName,
            user.Name.LastName,
            user.Role,
            user.IsActive,
            user.LastLoginAt);

        return new AuthResultModel(accessToken, refreshTokenValue, expiresAt, userModel);
    }
}
