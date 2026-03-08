using Enarro.Application.Abstractions;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enarro.Application.Services;

internal class ChatService : IChatService
{
    private readonly IVectorMemoryService _memory;
    private readonly IConversationStore _conversationService;
    private readonly ILogger<ChatService> _logger;
    private readonly string _index;
    private readonly int _maxHistoryMessages;

    public ChatService(
       IVectorMemoryService memory,
       IConversationStore conversationService,
       IConfiguration config,
       ILogger<ChatService> logger)
    {
        _memory = memory;
        _conversationService = conversationService;
        _logger = logger;
        _index = config["RAGConfigs:IndexName"] ?? "rag-test";
        _maxHistoryMessages = config.GetValue<int>("RAGConfigs:Conversation:MaxHistoryMessages", 10);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(Guid userId, ChatRequestModel request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Ensure session exists
        var sessionId = request.SessionId;
        var sessionValid = !string.IsNullOrWhiteSpace(sessionId);

        if (sessionValid)
        {
            var sessionResult = await _conversationService.SessionExistsAsync(sessionId!, cancellationToken);
            sessionValid = sessionResult.IsSuccess && sessionResult.Value;
        }

        if (!sessionValid)
        {
            var createResult = await _conversationService.CreateSessionAsync(UserId.From(userId), cancellationToken);
            if (createResult.IsFailure)
            {
                _logger.LogError("Failed to create session for streaming chat: {Error}", createResult.Error.Message);
                yield break;
            }
            sessionId = createResult.Value;
            _logger.LogInformation("Created new session {SessionId} for streaming chat", sessionId);
        }

        // 2. Retrieve conversation history
        var historyResult = await _conversationService.GetHistoryAsync(sessionId!, _maxHistoryMessages, cancellationToken);
        var history = historyResult.IsSuccess ? historyResult.Value : [];

        // 3. Build context-aware prompt
        var prompt = BuildPromptWithHistory(request.Message, history);

        // 4. Save user message to history
        await _conversationService.AddMessageAsync(sessionId!, "user", request.Message, cancellationToken);

        // 5. Stream the response
        var responseBuilder = new StringBuilder();

        // Note: Kernel Memory doesn't natively support streaming, so we'll use a workaround
        // In a production environment, you'd want to use the LLM directly for streaming
        var answer = await _memory.AskAsync(
            question: prompt,
            indexName: _index,
            filters: request.Filters,
            minRelevance: request.MinRelevance,
            cancellationToken: cancellationToken);

        var fullResponse = answer.IsSuccess ? answer.Value.Answer : "I don't know.";

        // Simulate streaming by chunking the response
        const int chunkSize = 10; // characters per chunk
        for (int i = 0; i < fullResponse.Length; i += chunkSize)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var chunk = fullResponse.Substring(i, Math.Min(chunkSize, fullResponse.Length - i));
            responseBuilder.Append(chunk);
            yield return chunk;

            // Small delay to simulate streaming
            await Task.Delay(20, cancellationToken);
        }

        // 6. Save assistant response to history
        await _conversationService.AddMessageAsync(sessionId!, "assistant", responseBuilder.ToString(), cancellationToken);

        _logger.LogInformation("Streaming chat completed for session {SessionId}", sessionId);
    }

    #region Private Method

    private static string BuildPromptWithHistory(string currentMessage, IReadOnlyList<ConversationMessageModel> history)
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("You are a helpful assistant. Use the following background information and conversation history to answer the user's question.");
        contextBuilder.AppendLine("If you don't know the answer based on the information, say 'I don't know'.");
        contextBuilder.AppendLine();

        if (history.Any())
        {
            contextBuilder.AppendLine("Conversation History:");
            foreach (var msg in history.TakeLast(5))
            {
                contextBuilder.AppendLine($"{msg.Role}: {msg.Content}");
            }
            contextBuilder.AppendLine();
        }

        contextBuilder.AppendLine($"Current Question: {currentMessage}");
        return contextBuilder.ToString();
    }

    #endregion
}