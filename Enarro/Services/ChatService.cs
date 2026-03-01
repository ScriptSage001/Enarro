using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.KernelMemory;
using CoreKernel.Functional.Results;
using Enarro.Common;
using Enarro.Common.Errors;
using Enarro.Contracts.Chat;
using ChatCitation = Enarro.Contracts.Chat.Citation;

namespace Enarro.Services;

public class ChatService : IChatService
{
    private readonly IKernelMemory _memory;
    private readonly IConversationService _conversationService;
    private readonly IUserContext _userContext;
    private readonly ILogger<ChatService> _logger;
    private readonly string _index;
    private readonly int _maxHistoryMessages;

    public ChatService(
        IKernelMemory memory,
        IConversationService conversationService,
        IUserContext userContext,
        IConfiguration config,
        ILogger<ChatService> logger)
    {
        _memory = memory;
        _conversationService = conversationService;
        _userContext = userContext;
        _logger = logger;
        _index = config["RAGConfigs:IndexName"] ?? "rag-test";
        _maxHistoryMessages = config.GetValue<int>("RAGConfigs:Conversation:MaxHistoryMessages", 10);
    }

    public async Task<Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Result.Failure<ChatResponse>(Errors.Chat.MessageEmpty());
        }

        if (request.MinRelevance < 0 || request.MinRelevance > 1)
        {
            return Result.Failure<ChatResponse>(Errors.Chat.InvalidRelevance());
        }

        try
        {
            // 2. Ensure session exists
            var sessionId = request.SessionId;
            var userId = _userContext.UserId?.ToString();
            if (string.IsNullOrEmpty(sessionId) || !await _conversationService.SessionExistsAsync(sessionId, cancellationToken))
            {
                var createResult = await _conversationService.CreateSessionAsync(userId, cancellationToken);
                if (createResult.IsFailure)
                {
                    return Result.Failure<ChatResponse>(createResult.Error);
                }
                sessionId = createResult.Value;
                _logger.LogInformation("Created new session {SessionId} for user {UserId}", sessionId, userId ?? "anonymous");
            }

            // 3. Retrieve conversation history
            var historyResult = await _conversationService.GetHistoryAsync(sessionId, _maxHistoryMessages, cancellationToken);
            var history = historyResult.IsSuccess ? historyResult.Value : [];

            // 4. Build context-aware prompt
            var prompt = BuildPromptWithHistory(request.Message, history);

            // 5. Perform RAG query
            var answer = await _memory.AskAsync(
                question: prompt,
                index: _index,
                filters: BuildFilters(request.Filters),
                minRelevance: request.MinRelevance,
                cancellationToken: cancellationToken);

            // 6. Extract citations
            var citations = ExtractCitations(answer);

            // 7. Create response
            var response = new ChatResponse(
                Answer: answer?.Result ?? "I don't know.",
                Citations: citations,
                Confidence: citations.Any() ? citations.Max(c => c.Relevance) : 0,
                SessionId: sessionId,
                TokensUsed: EstimateTokens(answer),
                Timestamp: DateTime.UtcNow
            );

            // 8. Save to conversation history
            await _conversationService.AddMessageAsync(sessionId,
                new ConversationMessage("user", request.Message, DateTime.UtcNow), cancellationToken);
            await _conversationService.AddMessageAsync(sessionId,
                new ConversationMessage("assistant", response.Answer, DateTime.UtcNow, citations), cancellationToken);

            _logger.LogInformation(
                "RAG Query completed: SessionId={SessionId}, Confidence={Confidence}, Citations={CitationCount}, Tokens={TokensUsed}",
                sessionId, response.Confidence, citations.Count, response.TokensUsed);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed");
            return Result.Failure<ChatResponse>(Errors.Chat.ProcessingFailed(ex.Message));
        }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Ensure session exists
        var sessionId = request.SessionId;
        var userId = _userContext.UserId?.ToString();
        if (string.IsNullOrEmpty(sessionId) || !await _conversationService.SessionExistsAsync(sessionId, cancellationToken))
        {
            var createResult = await _conversationService.CreateSessionAsync(userId, cancellationToken);
            if (createResult.IsFailure)
            {
                _logger.LogError("Failed to create session for streaming chat: {Error}", createResult.Error.Message);
                yield break;
            }
            sessionId = createResult.Value;
            _logger.LogInformation("Created new session {SessionId} for streaming chat", sessionId);
        }

        // 2. Retrieve conversation history
        var historyResult = await _conversationService.GetHistoryAsync(sessionId, _maxHistoryMessages, cancellationToken);
        var history = historyResult.IsSuccess ? historyResult.Value : [];

        // 3. Build context-aware prompt
        var prompt = BuildPromptWithHistory(request.Message, history);

        // 4. Save user message to history
        await _conversationService.AddMessageAsync(sessionId,
            new ConversationMessage("user", request.Message, DateTime.UtcNow), cancellationToken);

        // 5. Stream the response
        var responseBuilder = new StringBuilder();
        
        // Note: Kernel Memory doesn't natively support streaming, so we'll use a workaround
        // In a production environment, you'd want to use the LLM directly for streaming
        var answer = await _memory.AskAsync(
            question: prompt,
            index: _index,
            filters: BuildFilters(request.Filters),
            minRelevance: request.MinRelevance,
            cancellationToken: cancellationToken);

        var fullResponse = answer?.Result ?? "I don't know.";
        
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
        var citations = ExtractCitations(answer);
        await _conversationService.AddMessageAsync(sessionId,
            new ConversationMessage("assistant", responseBuilder.ToString(), DateTime.UtcNow, citations), cancellationToken);

        _logger.LogInformation("Streaming chat completed for session {SessionId}", sessionId);
    }

    private string BuildPromptWithHistory(string currentMessage, List<ConversationMessage> history)
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

    private List<ChatCitation> ExtractCitations(MemoryAnswer? answer)
    {
        if (answer?.RelevantSources == null || !answer.RelevantSources.Any())
            return new List<ChatCitation>();

        return answer.RelevantSources
            .SelectMany(source => source.Partitions.Select(partition => new ChatCitation(
                DocumentId: source.SourceName,
                DocumentName: source.SourceName,
                Excerpt: partition.Text.Length > 200 ? partition.Text[..200] + "..." : partition.Text,
                Relevance: partition.Relevance
            )))
            .OrderByDescending(c => c.Relevance)
            .Take(5)
            .ToList();
    }

    private int EstimateTokens(MemoryAnswer? answer)
    {
        // Rough estimation: 1 token ≈ 4 characters
        return (answer?.Result?.Length ?? 0) / 4;
    }

    private ICollection<MemoryFilter>? BuildFilters(Dictionary<string, string>? filters)
    {
        if (filters == null || !filters.Any()) return null;

        var memoryFilters = new List<MemoryFilter>();
        foreach (var filter in filters)
        {
            memoryFilters.Add(new MemoryFilter().ByTag(filter.Key, filter.Value));
        }
        return memoryFilters;
    }
}