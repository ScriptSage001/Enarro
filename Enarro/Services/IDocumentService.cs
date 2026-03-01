using CoreKernel.Functional.Results;
using Enarro.Contracts.Document;
using Enarro.Contracts.Common;

namespace Enarro.Services;

public interface IDocumentService
{
    Task<Result<DocumentIngestResult>> IngestAsync(IFormFile file, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);
    Task<Result<BatchIngestResult>> IngestBatchAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
    Task<Result<DocumentMetadata>> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<DocumentMetadata>>> ListDocumentsAsync(int page = 1, int pageSize = 20, string? tag = null, CancellationToken cancellationToken = default);
    Task<Result> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}
