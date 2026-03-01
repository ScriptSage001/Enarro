using Enarro.Application.Chat.Commands;
using Enarro.Extensions;
using Enarro.Contracts.Chat;
using MediatR;

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
        return result.ToHttpResult();
    }

    #endregion Private Methods
}