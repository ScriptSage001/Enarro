using CoreKernel.Functional.Results;
using Enarro.Application.Abstractions;
using Enarro.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

namespace Enarro.Infrastructure.AI;

/// <summary>
/// Vector memory service implementation using Microsoft Kernel Memory.
/// Handles document ingestion, deletion, and RAG queries.
/// Future: Can be swapped/extended for OpenAI, Gemini, or other providers.
/// </summary>
public class KernelMemoryVectorService(
    IKernelMemory kernelMemory,
    ILogger<KernelMemoryVectorService> logger) : IVectorMemoryService
{
    private const string DefaultIndex = "enarro";

    public async Task<Result<string>> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        string documentId,
        string? indexName = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagCollection = new TagCollection();
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    tagCollection.Add(tag.Key, tag.Value);
                }
            }

            var docId = await kernelMemory.ImportDocumentAsync(
                fileStream,
                fileName: fileName,
                documentId: documentId,
                tags: tagCollection,
                index: indexName ?? DefaultIndex,
                cancellationToken: cancellationToken);

            logger.LogInformation("Document {DocumentId} ingested as {DocId}", documentId, docId);

            return Result.Success(docId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ingest document {DocumentId}", documentId);
            return Result.Failure<string>(Error.Failure(
                "VectorMemory.IngestFailed",
                $"Ingestion failed: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteDocumentAsync(
        string documentId,
        string? indexName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await kernelMemory.DeleteDocumentAsync(
                documentId,
                index: indexName ?? DefaultIndex,
                cancellationToken: cancellationToken);

            logger.LogInformation("Document {DocumentId} deleted from vector store", documentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return Result.Failure(Error.Failure(
                "VectorMemory.DeleteFailed",
                $"Deletion failed: {ex.Message}"));
        }
    }

    public async Task<Result<VectorSearchResultModel>> AskAsync(
        string question,
        string? indexName = null,
        IEnumerable<KeyValuePair<string, string>>? filters = null,
        double minRelevance = 0,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var memoryFilter = new MemoryFilter();
            if (filters is not null)
            {
                foreach (var filter in filters)
                {
                    memoryFilter.Add(filter.Key, filter.Value);
                }
            }

            var answer = await kernelMemory.AskAsync(
                question,
                index: indexName ?? DefaultIndex,
                filters: [memoryFilter],
                minRelevance: minRelevance,
                cancellationToken: cancellationToken);

            var citations = answer.RelevantSources
                .Take(maxResults)
                .Select(s => new VectorCitationModel(
                    s.DocumentId,
                    s.SourceName ?? "Unknown",
                    string.Join(" ", s.Partitions.Select(p => p.Text).Take(1)),
                    s.Partitions.FirstOrDefault()?.Relevance ?? 0))
                .ToList();

            var isRelevant = !string.IsNullOrWhiteSpace(answer.Result)
                && !answer.Result.Contains("INFO NOT FOUND", StringComparison.OrdinalIgnoreCase);

            return Result.Success(new VectorSearchResultModel(
                answer.Result,
                citations,
                isRelevant));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query vector store");
            return Result.Failure<VectorSearchResultModel>(Error.Failure(
                "VectorMemory.QueryFailed",
                $"Query failed: {ex.Message}"));
        }
    }

    public async Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? indexName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await kernelMemory.IsDocumentReadyAsync(
                documentId,
                index: indexName ?? DefaultIndex,
                cancellationToken: cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
