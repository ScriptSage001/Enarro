using System.Security.Claims;
using Enarro.Models.Document;
using Enarro.Services;

namespace Enarro.Endpoints.Document;

public class Post : IEndpoint
{
    #region Public Endpoints

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app
            .MapPost("ingest", Ingest)
            .RequireAuthorization()
            .WithTags("Document")
            .WithSummary("Ingest a single document. Supported formats: TXT, PDF, DOCX, XLSX, PPTX, MD, JSON")
            .DisableAntiforgery()
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app
            .MapPost("ingest/batch", IngestBatch)
            .RequireAuthorization()
            .WithTags("Document")
            .WithSummary("Ingest multiple documents in one operation with parallel processing")
            .DisableAntiforgery()
            .Produces<BatchIngestResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> Ingest(
        IFormFile file,
        HttpContext httpContext,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (file.Length == 0)
                return Results.Problem("No file uploaded.", statusCode: 400);

            // Extract user ID from JWT claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.IngestAsync(file, userId, cancellationToken: cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> IngestBatch(
        IFormFileCollection files,
        HttpContext httpContext,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!files.Any())
                return Results.Problem("No files uploaded.", statusCode: 400);

            // Extract user ID from JWT claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.IngestBatchAsync(files, userId, cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message, statusCode: 500);
        }
    }

    #endregion Private Methods
}
