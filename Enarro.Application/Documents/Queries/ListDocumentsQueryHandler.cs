using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Abstractions;
using Enarro.Application.Common;
using Enarro.Application.Models;

namespace Enarro.Application.Documents.Queries;

public sealed class ListDocumentsQueryHandler(IDocumentQueryService documentQueryService)
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentModel>>
{
    public async Task<Result<PagedResult<DocumentModel>>> Handle(
        ListDocumentsQuery query, CancellationToken cancellationToken)
    {
        return await documentQueryService.GetPagedAsync(
            query.UserId, query.Page, query.PageSize, cancellationToken);
    }
}
