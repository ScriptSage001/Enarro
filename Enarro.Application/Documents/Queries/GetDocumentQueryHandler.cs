using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;

namespace Enarro.Application.Documents.Queries;

public sealed class GetDocumentQueryHandler(IDocumentQueryService documentQueryService)
    : IQueryHandler<GetDocumentQuery, DocumentModel>
{
    public async Task<Result<DocumentModel>> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        return await documentQueryService.GetByIdAsync(query.DocumentId, cancellationToken);
    }
}
