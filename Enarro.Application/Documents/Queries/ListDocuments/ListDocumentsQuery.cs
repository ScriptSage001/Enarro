using CoreKernel.Messaging.Queries;
using Enarro.Application.Common;
using Enarro.Application.Documents.DTOs;

namespace Enarro.Application.Documents.Queries.ListDocuments;

public sealed record ListDocumentsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null) : IQuery<PagedResult<DocumentDto>>;
