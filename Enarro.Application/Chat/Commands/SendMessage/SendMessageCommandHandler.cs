using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Chat.DTOs;

namespace Enarro.Application.Chat.Commands.SendMessage;

public sealed class SendMessageCommandHandler(
    IVectorMemoryService vectorMemoryService,
    IConversationStore conversationStore,
    ICurrentUserService currentUserService)
    : ICommandHandler<SendMessageCommand, ChatResultDto>
{
    public async Task<Result<ChatResultDto>> Handle(
        SendMessageCommand command, CancellationToken cancellationToken)
    {
        // Resolve or create session
        var sessionId = command.SessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            var userId = currentUserService.UserId ?? Guid.Empty;
            sessionId = await conversationStore.CreateSessionAsync(userId, cancellationToken);
        }

        // Store user message in history
        await conversationStore.AddMessageAsync(sessionId, "user", command.Message, cancellationToken);

        // Build context from conversation history
        var history = await conversationStore.GetHistoryAsync(sessionId, 10, cancellationToken);
        var contextBuilder = new System.Text.StringBuilder();
        foreach (var msg in history.Where(m => m.Role != "system"))
        {
            contextBuilder.AppendLine($"{msg.Role}: {msg.Content}");
        }

        var enrichedQuestion = history.Count > 1
            ? $"Previous conversation:\n{contextBuilder}\n\nCurrent question: {command.Message}"
            : command.Message;

        // Query vector store
        var filters = command.Filters?.Select(f =>
            new KeyValuePair<string, string>(f.Key, f.Value));

        var searchResult = await vectorMemoryService.AskAsync(
            enrichedQuestion,
            filters: filters,
            minRelevance: command.MinRelevance,
            maxResults: command.MaxResults,
            cancellationToken: cancellationToken);

        if (searchResult.IsFailure)
        {
            return Result.Failure<ChatResultDto>(searchResult.Error);
        }

        var result = searchResult.Value;

        // Store assistant response in history
        await conversationStore.AddMessageAsync(sessionId, "assistant", result.Answer, cancellationToken);

        // Map to DTO
        var citations = result.Citations.Select(c =>
            new CitationDto(c.DocumentId, c.FileName, c.Excerpt, c.Relevance)).ToList();

        var confidenceScore = result.Citations.Count > 0
            ? result.Citations.Average(c => c.Relevance)
            : 0.0;

        return Result.Success(new ChatResultDto(
            result.Answer,
            citations,
            confidenceScore,
            sessionId,
            0));
    }
}
