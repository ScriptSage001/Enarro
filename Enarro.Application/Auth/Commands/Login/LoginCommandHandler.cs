using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Auth.DTOs;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<LoginCommand, AuthResult>
{
    public async Task<Result<AuthResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResult>(UserErrors.InvalidCredentials());
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResult>(UserErrors.UserInactive());
        }

        if (!BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResult>(UserErrors.InvalidCredentials());
        }

        user.UpdateLastLogin();

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id.Value, user.Email.Value, user.Role);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshTokenValue, jwtTokenService.RefreshTokenExpirationDays);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtTokenService.AccessTokenExpirationMinutes);

        var userDto = new UserDto(
            user.Id.Value,
            user.Email.Value,
            user.Name.FirstName,
            user.Name.LastName,
            user.Role,
            user.IsActive,
            user.LastLoginAt);

        return Result.Success(new AuthResult(accessToken, refreshTokenValue, expiresAt, userDto));
    }
}
