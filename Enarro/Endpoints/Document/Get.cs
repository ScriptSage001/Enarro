using Enarro.Application.Documents.Queries;
using Enarro.Extensions;
using MediatR;

namespace Enarro.Endpoints.Document;

public class Get : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app
            .MapGet("documents", ListDocuments)
            .RequireAuthorization()
            .WithTags("Document")
            .WithSummary("List all documents with optional filtering and pagination")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app
            .MapGet("documents/{id}", GetDocument)
            .RequireAuthorization()
            .WithTags("Document")
            .WithSummary("Get document details by ID")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> ListDocuments(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListDocumentsQuery(page, pageSize);
        var result = await sender.Send(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDocument(
        string id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetDocumentQuery(id);
        var result = await sender.Send(query, cancellationToken);
        return result.ToHttpResult();
    }

    #endregion Private Methods
}
