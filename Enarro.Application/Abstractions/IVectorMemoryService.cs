using CoreKernel.Functional.Results;
using Enarro.Application.Models;

namespace Enarro.Application.Abstractions;

/// <summary>
/// Contract for vector memory store operations (ingestion + retrieval).
/// Abstracts over Kernel Memory and future AI providers (OpenAI, Gemini).
/// </summary>
public interface IVectorMemoryService
{
    /// <summary>
    /// Ingests a document into the vector store.
    /// </summary>
    Task<Result<string>> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        string documentId,
        string? indexName = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from the vector store.
    /// </summary>
    Task<Result> DeleteDocumentAsync(
        string documentId,
        string? indexName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a question against the indexed documents and returns an answer with citations.
    /// </summary>
    Task<Result<VectorSearchResultModel>> AskAsync(
        string question,
        string? indexName = null,
        IEnumerable<KeyValuePair<string, string>>? filters = null,
        double minRelevance = 0,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document is ready (fully indexed) in the vector store.
    /// </summary>
    Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? indexName = null,
        CancellationToken cancellationToken = default);
}
