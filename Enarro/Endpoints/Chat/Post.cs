using System.Security.Claims;
using Enarro.Models.Chat;
using Enarro.Services;

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
                .Produces<ChatResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> Chat(
        ChatRequest request,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.Problem("Message cannot be empty.", statusCode: 400);
            }

            if (request.MinRelevance < 0 || request.MinRelevance > 1)
            {
                return Results.Problem("MinRelevance must be between 0 and 1.", statusCode: 400);
            }

            var result = await chatService.ChatAsync(request, cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message, statusCode: 500);
        }
    }

    #endregion Private Methods
}
