namespace Enarro.Models.Search;

/// <summary>
/// Search result from keyword or vector search
/// </summary>
public class HybridSearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Statistics about search operations
/// </summary>
public class SearchStatistics
{
    public int TotalDocuments { get; set; }
    public int TotalTerms { get; set; }
    public double AverageDocumentLength { get; set; }
    public DateTime LastIndexedAt { get; set; }
}
