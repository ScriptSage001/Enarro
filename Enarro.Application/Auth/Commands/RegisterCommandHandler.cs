using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Commands;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<RegisterCommand, AuthResult>
{
    public async Task<Result<AuthResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsAsync(command.Email, cancellationToken))
        {
            return Result.Failure<AuthResult>(UserErrors.EmailAlreadyExists(command.Email));
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

        var user = User.Register(
            command.Email,
            passwordHash,
            command.FirstName,
            command.LastName);

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id.Value, command.Email, user.Role);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshTokenValue, jwtTokenService.RefreshTokenExpirationDays);

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userModel = new UserModel(
            user.Id.Value,
            user.Email.Value,
            user.Name.FirstName,
            user.Name.LastName,
            user.Role,
            user.IsActive,
            user.LastLoginAt);

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtTokenService.AccessTokenExpirationMinutes);

        return new AuthResult(accessToken, refreshTokenValue, expiresAt, userModel);
    }
}
