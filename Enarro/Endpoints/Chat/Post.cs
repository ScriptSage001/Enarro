using Enarro.Extensions;
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
        var result = await chatService.ChatAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    #endregion Private Methods
}