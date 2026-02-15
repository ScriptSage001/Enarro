using Enarro.Extensions;
using Enarro.Models.Document;
using Enarro.Services;

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
            .Produces<DocumentMetadata>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> ListDocuments(
        IDocumentService documentService,
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        var result = await documentService.ListDocumentsAsync(page, pageSize, tag, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDocument(
        string id,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var result = await documentService.GetDocumentAsync(id, cancellationToken);
        return result.ToHttpResult();
    }

    #endregion Private Methods
}
