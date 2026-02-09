namespace Enarro.Services.Search;

using Enarro.Models.Search;

/// <summary>
/// Service for keyword-based search using BM25 algorithm
/// </summary>
public interface IKeywordSearchService
{
    /// <summary>
    /// Index a document for keyword search
    /// </summary>
    Task IndexDocumentAsync(string documentId, string content, CancellationToken ct = default);
    
    /// <summary>
    /// Search for documents using keyword matching
    /// </summary>
    Task<List<HybridSearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default);
    
    /// <summary>
    /// Remove a document from the index
    /// </summary>
    Task RemoveDocumentAsync(string documentId, CancellationToken ct = default);
    
    /// <summary>
    /// Get statistics about the search index
    /// </summary>
    Task<SearchStatistics> GetStatisticsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Clear all indexed documents
    /// </summary>
    Task ClearIndexAsync(CancellationToken ct = default);
}
