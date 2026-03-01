using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Chat.Models;

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
            sessionId = await conversationStore.CreateSessionAsync(userId, cancellationToken);
        }

        await conversationStore.AddMessageAsync(sessionId, "user", command.Message, cancellationToken);

        var history = await conversationStore.GetHistoryAsync(sessionId, 10, cancellationToken);
        var contextBuilder = new System.Text.StringBuilder();
        foreach (var msg in history.Where(m => m.Role != "system"))
        {
            contextBuilder.AppendLine($"{msg.Role}: {msg.Content}");
        }

        var enrichedQuestion = history.Count > 1
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
