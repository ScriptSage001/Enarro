using Enarro.Application.Abstractions;
using Enarro.Application.Documents.Commands;
using Enarro.Application.Models;
using Enarro.Extensions;
using MediatR;

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
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #endregion Public Endpoints

    #region Private Methods

    private static async Task<IResult> Ingest(
        IFormFile file,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var stream = file.OpenReadStream();
        var command = new IngestDocumentCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            currentUser.UserId);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> IngestBatch(
        IFormFileCollection files,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var results = new List<DocumentIngestResultModel>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var file in files)
        {
            var stream = file.OpenReadStream();
            var command = new IngestDocumentCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                currentUser.UserId);

            var result = await sender.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                results.Add(result.Value);
                successCount++;
            }
            else
            {
                results.Add(new DocumentIngestResultModel(
                    string.Empty, file.FileName, "Failed", result.Error.Message));
                failureCount++;
            }
        }

        var batchResult = new BatchIngestResultModel(
            files.Count, successCount, failureCount, results);

        return Results.Ok(batchResult);
    }

    #endregion Private Methods
}
