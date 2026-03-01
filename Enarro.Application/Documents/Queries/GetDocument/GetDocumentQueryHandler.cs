using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Documents.DTOs;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;

namespace Enarro.Application.Documents.Queries.GetDocument;

public sealed class GetDocumentQueryHandler(IDocumentRepository documentRepository)
    : IQueryHandler<GetDocumentQuery, DocumentDto>
{
    public async Task<Result<DocumentDto>> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query.DocumentId, out var docGuid))
        {
            return Result.Failure<DocumentDto>(DocumentErrors.NotFound(query.DocumentId));
        }

        var document = await documentRepository.GetByIdAsync(
            DocumentId.From(docGuid), cancellationToken);

        if (document is null)
        {
            return Result.Failure<DocumentDto>(DocumentErrors.NotFound(query.DocumentId));
        }

        return Result.Success(MapToDto(document));
    }

    private static DocumentDto MapToDto(Document doc) => new(
        doc.Id.Value,
        doc.FileName,
        doc.ContentType,
        doc.SizeBytes,
        doc.Status.ToString(),
        doc.UploadedAt,
        doc.UserId,
        doc.ChunkCount,
        doc.ErrorMessage,
        doc.Tags.Select(t => new DocumentTagDto(t.TagKey, t.TagValue)).ToList());
}
