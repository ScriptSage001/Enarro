using CoreKernel.Functional.Results;
using Enarro.Application.Common;
using Enarro.Application.Models;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Abstraction for document read-side queries.
/// Implemented in the Persistence layer to keep EF Core out of Application.
/// </summary>
public interface IDocumentQueryService
{
    Task<Result<PagedResult<DocumentModel>>> GetPagedAsync(
        Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<DocumentModel>> GetByIdAsync(
        string documentId, CancellationToken cancellationToken = default);
}
