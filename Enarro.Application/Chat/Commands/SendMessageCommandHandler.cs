using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;

namespace Enarro.Application.Chat.Commands;

public sealed class SendMessageCommandHandler(
    IVectorMemoryService vectorMemoryService,
    IConversationStore conversationStore,
    ICurrentUserService currentUserService)
    : ICommandHandler<SendMessageCommand, ChatResultModel>
{
    public async Task<Result<ChatResultModel>> Handle(
        SendMessageCommand command, CancellationToken cancellationToken)
    {
        var sessionId = command.SessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            var userId = currentUserService.UserId ?? Guid.Empty;
            var sessionIdResult = await conversationStore.CreateSessionAsync(userId, cancellationToken);

            if (sessionIdResult.IsFailure)
            {
                // log
                return Result.Failure<ChatResultModel>(sessionIdResult.Error);
            }

            sessionId = sessionIdResult.Value;
        }

        await conversationStore.AddMessageAsync(sessionId, "user", command.Message, cancellationToken);

        var historyResult = await conversationStore.GetHistoryAsync(sessionId, 10, cancellationToken);
        var messages = historyResult.IsSuccess ? historyResult.Value : [];
        var contextBuilder = new System.Text.StringBuilder();
        foreach (var msg in messages.Where(m => m.Role != "system"))
        {
            contextBuilder.AppendLine($"{msg.Role}: {msg.Content}");
        }

        var enrichedQuestion = messages.Count > 1
            ? $"Previous conversation:\n{contextBuilder}\n\nCurrent question: {command.Message}"
            : command.Message;

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
            return Result.Failure<ChatResultModel>(searchResult.Error);
        }

        var result = searchResult.Value;

        await conversationStore.AddMessageAsync(sessionId, "assistant", result.Answer, cancellationToken);

        var citations = result.Citations.Select(c =>
            new CitationModel(c.DocumentId, c.FileName, c.Excerpt, c.Relevance)).ToList();

        var confidenceScore = result.Citations.Count > 0
            ? result.Citations.Average(c => c.Relevance)
            : 0.0;

        return new ChatResultModel(
            result.Answer,
            citations,
            confidenceScore,
            sessionId,
            0);
    }
}
