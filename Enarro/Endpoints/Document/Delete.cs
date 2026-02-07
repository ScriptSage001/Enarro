using Enarro.Services;

namespace Enarro.Endpoints.Document;

public class Delete : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app
            .MapDelete("documents/{id}", DeleteDocument)
            .WithTags("Document")
            .WithSummary("Delete a document from the index")
            .Produces<object>(StatusCodes.Status200OK)
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
        try
        {
            var success = await documentService.DeleteDocumentAsync(id, cancellationToken);
            
            if (!success)
                return Results.Problem("Failed to delete document or document not found.", statusCode: 404);

            return Results.Ok(new { DocumentId = id, Status = "Deleted" });
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message, statusCode: 500);
        }
    }

    #endregion Private Methods
}
