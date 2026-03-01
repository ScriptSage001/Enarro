using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Application.Documents.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;

namespace Enarro.Application.Documents.Commands;

public sealed class IngestDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IVectorMemoryService vectorMemoryService)
    : ICommandHandler<IngestDocumentCommand, DocumentIngestResultModel>
{
    public async Task<Result<DocumentIngestResultModel>> Handle(
        IngestDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = Document.Create(
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.UserId,
            command.Tags?.Select(t => new KeyValuePair<string, string>(t.Key, t.Value)));

        document.MarkAsProcessing();

        documentRepository.Add(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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

            return new DocumentIngestResultModel(
                document.Id.Value.ToString(),
                command.FileName,
                "Failed",
                ingestResult.Error.Message);
        }

        document.MarkAsIndexed();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentIngestResultModel(
            document.Id.Value.ToString(),
            command.FileName,
            "Indexed",
            null);
    }
}
