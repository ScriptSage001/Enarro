using Enarro.Models.Document;
using Enarro.Models.Common;

namespace Enarro.Services;

public interface IDocumentService
{
    Task<DocumentIngestResult> IngestAsync(IFormFile file, Guid userId, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);
    Task<BatchIngestResult> IngestBatchAsync(IEnumerable<IFormFile> files, Guid userId, CancellationToken cancellationToken = default);
    Task<DocumentMetadata?> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<PagedResult<DocumentMetadata>> ListDocumentsAsync(int page = 1, int pageSize = 20, string? tag = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}
