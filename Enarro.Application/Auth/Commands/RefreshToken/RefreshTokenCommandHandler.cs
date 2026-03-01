using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Auth.DTOs;
using Enarro.Domain.Common;
using Enarro.Domain.Users;

namespace Enarro.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshTokenAsync(command.RefreshToken, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResult>(UserErrors.TokenNotFound());
        }

        var existingToken = user.RefreshTokens
            .FirstOrDefault(rt => rt.Token == command.RefreshToken);

        if (existingToken is null || !existingToken.IsValid())
        {
            return Result.Failure<AuthResult>(UserErrors.InvalidToken());
        }

        existingToken.Revoke("Replaced by new token");

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id.Value, user.Email.Value, user.Role);
        var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();

        user.AddRefreshToken(newRefreshTokenValue, jwtTokenService.RefreshTokenExpirationDays);

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

        return Result.Success(new AuthResult(accessToken, newRefreshTokenValue, expiresAt, userDto));
    }
}
