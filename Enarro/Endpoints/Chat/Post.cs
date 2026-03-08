using Enarro.Application.Abstractions;
using Enarro.Application.Chat.Commands;
using Enarro.Application.Models;
using Enarro.Application.Services;
using Enarro.Contracts.Chat;
using Enarro.Extensions;
using MediatR;
using static Enarro.Errors.Errors;

namespace Enarro.Endpoints.Chat;

public class Post : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("chat", Chat)
                .RequireAuthorization()
                .WithTags("Chat")
                .WithSummary("Chat endpoint with conversation history and citations support")
                .Produces<object>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPost("chat/stream", ChatStream)
                .RequireAuthorization()
                .WithTags("Chat")
                .WithSummary("Stream chat responses in real-time using Server-Sent Events")
                .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> Chat(
        ChatRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(
            request.Message,
            request.SessionId,
            request.Filters,
            request.MinRelevance,
            request.MaxResults);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult((val) => Results.Ok(FromModel(val)));
    }

    private static async Task<IResult> ChatStream(
        ChatRequest request,
        IChatService chatService,
        ICurrentUserService currentUserService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (userId == null) 
        {
            return UserNotResolved().ToHttpResult();
        }

        // Validate input before starting the stream
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return EmptyChatMessage().ToHttpResult();
        }

        // Set headers for Server-Sent Events
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await foreach (var chunk in chatService.ChatStreamAsync(userId.Value, ToModel(request), cancellationToken))
        {
            var data = $"data: {chunk}\n\n";
            await context.Response.WriteAsync(data, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        // Send completion event
        await context.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        return Results.Empty;
    }

    private static ChatResponse FromModel(ChatResultModel model)
    {
        var citations = model.Citations.Select(x => FromModel(x)).ToList();
        return new ChatResponse(model.Answer, citations, model.ConfidenceScore, model.SessionId, model.TokensUsed);
    }

    private static Citation FromModel(CitationModel model)
        => new(model.DocumentId, model.FileName, model.Excerpt, model.Relevance);

    private static ChatRequestModel ToModel(ChatRequest dto)
        => new(dto.Message, dto.SessionId, dto.Filters, dto.MaxResults, dto.MinRelevance);

    #endregion Private Methods
}