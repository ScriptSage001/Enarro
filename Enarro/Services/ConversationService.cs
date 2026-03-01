using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using CoreKernel.Functional.Results;
using Enarro.Common.Errors;
using Enarro.Contracts.Chat;
using Enarro.Contracts.Session;

namespace Enarro.Services;

public class ConversationService : IConversationService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConversationService> _logger;
    private readonly TimeSpan _sessionTimeout;
    private readonly TimeSpan _absoluteExpiration;

    public ConversationService(
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<ConversationService> logger)
    {
        _cache = cache;
        _logger = logger;
        _sessionTimeout = TimeSpan.FromMinutes(
            configuration.GetValue<int>("RAGConfigs:Conversation:SessionTimeoutMinutes", 60));
        _absoluteExpiration = TimeSpan.FromHours(24);
    }

    public async Task<Result<string>> CreateSessionAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionInfo = new SessionInfo(sessionId, userId, DateTime.UtcNow, DateTime.UtcNow);

            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = _sessionTimeout,
                AbsoluteExpirationRelativeToNow = _absoluteExpiration
            };

            await _cache.SetStringAsync(
                $"session:{sessionId}",
                JsonSerializer.Serialize(sessionInfo),
                options,
                cancellationToken);

            await _cache.SetStringAsync(
                $"messages:{sessionId}",
                JsonSerializer.Serialize(new List<ConversationMessage>()),
                options,
                cancellationToken);

            _logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId ?? "anonymous");
            return Result.Success(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            return Result.Failure<string>(Errors.Conversation.CreationFailed(ex.Message));
        }
    }

    public async Task<Result<List<ConversationMessage>>> GetHistoryAsync(
        string sessionId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messagesJson = await _cache.GetStringAsync($"messages:{sessionId}", cancellationToken);

            if (string.IsNullOrEmpty(messagesJson))
            {
                _logger.LogWarning("No history found for session {SessionId}", sessionId);
                return Result.Success(new List<ConversationMessage>());
            }

            var messages = JsonSerializer.Deserialize<List<ConversationMessage>>(messagesJson) ?? new List<ConversationMessage>();

            // Update last accessed time
            await TouchSessionAsync(sessionId, cancellationToken);

            return Result.Success(messages.TakeLast(limit).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history for session {SessionId}", sessionId);
            return Result.Failure<List<ConversationMessage>>(Errors.Internal(ex.Message));
        }
    }

    public async Task<Result> AddMessageAsync(
        string sessionId,
        ConversationMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messagesJson = await _cache.GetStringAsync($"messages:{sessionId}", cancellationToken);
            var messages = string.IsNullOrEmpty(messagesJson)
                ? []
                : JsonSerializer.Deserialize<List<ConversationMessage>>(messagesJson) ?? [];

            messages.Add(message);

            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = _sessionTimeout,
                AbsoluteExpirationRelativeToNow = _absoluteExpiration
            };

            await _cache.SetStringAsync(
                $"messages:{sessionId}",
                JsonSerializer.Serialize(messages),
                options,
                cancellationToken);

            // Update last accessed time
            await TouchSessionAsync(sessionId, cancellationToken);

            _logger.LogDebug("Added {Role} message to session {SessionId}", message.Role, sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message to session {SessionId}", sessionId);
            return Result.Failure(Errors.Internal(ex.Message));
        }
    }

    public async Task<Result> ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync($"session:{sessionId}", cancellationToken);
            await _cache.RemoveAsync($"messages:{sessionId}", cancellationToken);

            _logger.LogInformation("Cleared session {SessionId}", sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session {SessionId}", sessionId);
            return Result.Failure(Errors.Internal(ex.Message));
        }
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionJson = await _cache.GetStringAsync($"session:{sessionId}", cancellationToken);
        return !string.IsNullOrEmpty(sessionJson);
    }

    public async Task<Result<SessionInfo>> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionJson = await _cache.GetStringAsync($"session:{sessionId}", cancellationToken);

        if (string.IsNullOrEmpty(sessionJson))
        {
            return Result.Failure<SessionInfo>(Errors.Conversation.SessionNotFound(sessionId));
        }

        var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(sessionJson);
        if (sessionInfo == null)
        {
            return Result.Failure<SessionInfo>(Errors.Internal($"Failed to deserialize session {sessionId}"));
        }

        // Update last accessed time
        await TouchSessionAsync(sessionId, cancellationToken);

        return Result.Success(sessionInfo);
    }

    private async Task TouchSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var sessionJson = await _cache.GetStringAsync($"session:{sessionId}", cancellationToken);
        if (string.IsNullOrEmpty(sessionJson))
            return;

        var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(sessionJson);
        if (sessionInfo == null)
            return;

        var updatedSessionInfo = sessionInfo with { LastAccessedAt = DateTime.UtcNow };

        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = _sessionTimeout,
            AbsoluteExpirationRelativeToNow = _absoluteExpiration
        };

        await _cache.SetStringAsync(
            $"session:{sessionId}",
            JsonSerializer.Serialize(updatedSessionInfo),
            options,
            cancellationToken);
    }
}
