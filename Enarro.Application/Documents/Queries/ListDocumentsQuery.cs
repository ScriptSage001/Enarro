using CoreKernel.Messaging.Queries;
using Enarro.Application.Common;
using Enarro.Application.Documents.Models;

namespace Enarro.Application.Documents.Queries;

public sealed record ListDocumentsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null) : IQuery<PagedResult<DocumentModel>>;
