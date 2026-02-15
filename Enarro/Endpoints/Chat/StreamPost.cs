using Enarro.Common.Errors;
using Enarro.Models.Chat;
using Enarro.Services;

namespace Enarro.Endpoints.Chat;

public class StreamPost : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
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

    private static async Task<IResult> ChatStream(
        ChatRequest request,
        IChatService chatService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Validate input before starting the stream
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new { code = ErrorCodes.ChatMessageEmpty, error = "Message cannot be empty" });
        }

        // Set headers for Server-Sent Events
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await foreach (var chunk in chatService.ChatStreamAsync(request, cancellationToken))
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

    #endregion Private Methods
}