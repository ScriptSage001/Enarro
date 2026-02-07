using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.KernelMemory;
using Enarro.Models.Chat;

namespace Enarro.Services;

public class ChatService : IChatService
{
    private readonly IKernelMemory _memory;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatService> _logger;
    private readonly string _index;
    private readonly int _maxHistoryMessages;

    public ChatService(
        IKernelMemory memory,
        IConversationService conversationService,
        IConfiguration config,
        ILogger<ChatService> logger)
    {
        _memory = memory;
        _conversationService = conversationService;
        _logger = logger;
        _index = config["RAGConfigs:IndexName"] ?? "rag-test";
        _maxHistoryMessages = config.GetValue<int>("RAGConfigs:Conversation:MaxHistoryMessages", 10);
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        // 1. Ensure session exists
        var sessionId = request.SessionId;
        if (string.IsNullOrEmpty(sessionId) || !await _conversationService.SessionExistsAsync(sessionId, cancellationToken))
        {
            sessionId = await _conversationService.CreateSessionAsync(request.UserId, cancellationToken);
            _logger.LogInformation("Created new session {SessionId} for user {UserId}", sessionId, request.UserId ?? "anonymous");
        }

        // 2. Retrieve conversation history
        var history = await _conversationService.GetHistoryAsync(sessionId, _maxHistoryMessages, cancellationToken);

        // 3. Build context-aware prompt
        var prompt = BuildPromptWithHistory(request.Message, history);

        // 4. Perform RAG query
        var answer = await _memory.AskAsync(
            question: prompt,
            index: _index,
            filters: BuildFilters(request.Filters),
            minRelevance: request.MinRelevance,
            cancellationToken: cancellationToken);

        // 5. Extract citations
        var citations = ExtractCitations(answer);

        // 6. Create response
        var response = new ChatResponse(
            Answer: answer?.Result ?? "I don't know.",
            Citations: citations,
            Confidence: citations.Any() ? citations.Max(c => c.Relevance) : 0,
            SessionId: sessionId,
            TokensUsed: EstimateTokens(answer),
            Timestamp: DateTime.UtcNow
        );

        // 7. Save to conversation history
        await _conversationService.AddMessageAsync(sessionId,
            new ConversationMessage("user", request.Message, DateTime.UtcNow), cancellationToken);
        await _conversationService.AddMessageAsync(sessionId,
            new ConversationMessage("assistant", response.Answer, DateTime.UtcNow, citations), cancellationToken);

        _logger.LogInformation(
            "RAG Query completed: SessionId={SessionId}, Confidence={Confidence}, Citations={CitationCount}, Tokens={TokensUsed}",
            sessionId, response.Confidence, citations.Count, response.TokensUsed);

        return response;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Ensure session exists
        var sessionId = request.SessionId;
        if (string.IsNullOrEmpty(sessionId) || !await _conversationService.SessionExistsAsync(sessionId, cancellationToken))
        {
            sessionId = await _conversationService.CreateSessionAsync(request.UserId, cancellationToken);
            _logger.LogInformation("Created new session {SessionId} for streaming chat", sessionId);
        }

        // 2. Retrieve conversation history
        var history = await _conversationService.GetHistoryAsync(sessionId, _maxHistoryMessages, cancellationToken);

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

    private List<Models.Chat.Citation> ExtractCitations(MemoryAnswer? answer)
    {
        if (answer?.RelevantSources == null || !answer.RelevantSources.Any())
            return new List<Models.Chat.Citation>();

        return answer.RelevantSources
            .SelectMany(source => source.Partitions.Select(partition => new Models.Chat.Citation(
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