using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Documents.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;

namespace Enarro.Application.Documents.Queries;

public sealed class GetDocumentQueryHandler(IDocumentRepository documentRepository)
    : IQueryHandler<GetDocumentQuery, DocumentModel>
{
    public async Task<Result<DocumentModel>> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query.DocumentId, out var docGuid))
        {
            return Result.Failure<DocumentModel>(DocumentErrors.NotFound(query.DocumentId));
        }

        var document = await documentRepository.GetByIdAsync(
            DocumentId.From(docGuid), cancellationToken);

        if (document is null)
        {
            return Result.Failure<DocumentModel>(DocumentErrors.NotFound(query.DocumentId));
        }

        return MapToModel(document);
    }

    private static DocumentModel MapToModel(Document doc) => new(
        doc.Id.Value,
        doc.FileName,
        doc.ContentType,
        doc.SizeBytes,
        doc.Status.ToString(),
        doc.UploadedAt,
        doc.UserId,
        doc.ChunkCount,
        doc.ErrorMessage,
        doc.Tags.Select(t => new DocumentTagModel(t.TagKey, t.TagValue)).ToList());
}
