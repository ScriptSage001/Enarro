using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Documents.DTOs;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;

namespace Enarro.Application.Documents.Commands.Ingest;

public sealed class IngestDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IVectorMemoryService vectorMemoryService)
    : ICommandHandler<IngestDocumentCommand, DocumentIngestResultDto>
{
    public async Task<Result<DocumentIngestResultDto>> Handle(
        IngestDocumentCommand command, CancellationToken cancellationToken)
    {
        // Create domain entity
        var document = Document.Create(
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.UserId,
            command.Tags?.Select(t => new KeyValuePair<string, string>(t.Key, t.Value)));

        document.MarkAsProcessing();

        documentRepository.Add(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Ingest into vector store
        var tags = new Dictionary<string, string>
        {
            ["documentId"] = document.Id.Value.ToString(),
            ["userId"] = command.UserId?.ToString() ?? "system"
        };

        if (command.Tags is not null)
        {
            foreach (var tag in command.Tags)
            {
                tags[tag.Key] = tag.Value;
            }
        }

        var ingestResult = await vectorMemoryService.IngestDocumentAsync(
            command.FileStream,
            command.FileName,
            document.Id.Value.ToString(),
            tags: tags,
            cancellationToken: cancellationToken);

        if (ingestResult.IsFailure)
        {
            document.MarkAsFailed(ingestResult.Error.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new DocumentIngestResultDto(
                document.Id.Value.ToString(),
                command.FileName,
                "Failed",
                ingestResult.Error.Message));
        }

        document.MarkAsIndexed();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DocumentIngestResultDto(
            document.Id.Value.ToString(),
            command.FileName,
            "Indexed",
            null));
    }
}
