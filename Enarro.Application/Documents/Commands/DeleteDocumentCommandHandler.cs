using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Commands;
using Enarro.Application.Abstractions;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;

namespace Enarro.Application.Documents.Commands;

public sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IVectorMemoryService vectorMemoryService)
    : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Result> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.DocumentId, out var docGuid))
        {
            return Result.Failure(DocumentErrors.NotFound(command.DocumentId));
        }

        var document = await documentRepository.GetByIdAsync(
            DocumentId.From(docGuid), cancellationToken);

        if (document is null)
        {
            return Result.Failure(DocumentErrors.NotFound(command.DocumentId));
        }

        var deleteResult = await vectorMemoryService.DeleteDocumentAsync(
            command.DocumentId, cancellationToken: cancellationToken);

        if (deleteResult.IsFailure)
        {
            return Result.Failure(DocumentErrors.DeleteFailed(command.DocumentId, deleteResult.Error.Message));
        }

        document.MarkAsDeleted();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
