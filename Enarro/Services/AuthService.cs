using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using CoreKernel.Functional.Results;
using Enarro.Common.Errors;
using Enarro.Data;
using Enarro.Data.Entities;
using Enarro.Contracts.Auth;

namespace Enarro.Services;

/// <summary>
/// Service for authentication and user management
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        
        _jwtSecret = configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        _jwtIssuer = configuration["JwtSettings:Issuer"] ?? "Enarro";
        _jwtAudience = configuration["JwtSettings:Audience"] ?? "EnarroAPI";
        _accessTokenExpirationMinutes = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes", 15);
        _refreshTokenExpirationDays = configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Validate email is unique
        var existingUser = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken: cancellationToken);
            
        if (existingUser != null)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.EmailAlreadyExists(request.Email));
        }

        // Validate password strength
        var passwordValidation = ValidatePassword(request.Password);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<AuthResponse>(passwordValidation.Error);
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        // Create user entity
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "User",
            IsActive = true
        };

        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        // Generate tokens
        return Result.Success(await GenerateAuthResponseAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Find user by email
        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken: cancellationToken);

        if (user == null)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.InvalidCredentials());
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Result.Failure<AuthResponse>(Errors.Auth.InvalidCredentials());
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.UserInactive());
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        // Generate tokens
        return Result.Success(await GenerateAuthResponseAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Find refresh token
        var tokenEntity = await _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == refreshToken,
                include: q => q.Include(rt => rt.User),
                cancellationToken: cancellationToken);

        if (tokenEntity == null)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.InvalidToken());
        }

        // Check if token is expired
        if (tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.TokenExpired());
        }

        // Check if token is revoked
        if (tokenEntity.IsRevoked)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.InvalidToken());
        }

        // Check if user is active
        if (!tokenEntity.User.IsActive)
        {
            return Result.Failure<AuthResponse>(Errors.Auth.UserInactive());
        }

        // Revoke old refresh token
        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedReason = "Token refreshed";

        // Generate new tokens
        var authResponse = await GenerateAuthResponseAsync(tokenEntity.User, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token used for user: {Email}", tokenEntity.User.Email);

        return Result.Success(authResponse);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tokenEntity = await _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken: cancellationToken);

        if (tokenEntity == null)
        {
            return Result.Failure(Errors.Auth.TokenNotFound());
        }

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedReason = reason ?? "Manually revoked";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token revoked: {Reason}", reason);
        return Result.Success();
    }

    public async Task<Result<UserInfo>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

        if (user == null)
        {
            return Result.Failure<UserInfo>(Errors.Auth.UserNotFound(userId.ToString()));
        }

        return Result.Success(new UserInfo(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        ));
    }

    #region Private Methods

    private async Task<AuthResponse> GenerateAuthResponseAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        // Save refresh token
        var refreshTokenEntity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            IsRevoked = false
        };

        _unitOfWork.RefreshTokens.Add(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            User: new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            )
        );
    }

    private string GenerateJwtToken(UserEntity user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private Result ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Failure(Errors.Auth.WeakPassword("Password is required"));

        if (password.Length < 8)
            return Result.Failure(Errors.Auth.WeakPassword("Password must be at least 8 characters"));

        if (!password.Any(char.IsUpper))
            return Result.Failure(Errors.Auth.WeakPassword("Password must contain at least one uppercase letter"));

        if (!password.Any(char.IsLower))
            return Result.Failure(Errors.Auth.WeakPassword("Password must contain at least one lowercase letter"));

        if (!password.Any(char.IsDigit))
            return Result.Failure(Errors.Auth.WeakPassword("Password must contain at least one digit"));

        return Result.Success();
    }

    #endregion
}
