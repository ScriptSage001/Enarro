using Enarro.Extensions;
using Enarro.Services;

namespace Enarro.Endpoints.Document;

public class Delete : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app
            .MapDelete("documents/{id}", DeleteDocument)
            .RequireAuthorization()
            .WithTags("Document")
            .WithSummary("Delete a document from the index")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> DeleteDocument(
        string id,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var result = await documentService.DeleteDocumentAsync(id, cancellationToken);
        return result.ToHttpResult(() => Results.Ok(new { DocumentId = id, Status = "Deleted" }));
    }

    #endregion Private Methods
}
