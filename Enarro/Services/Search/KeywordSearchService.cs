namespace Enarro.Services.Search;

using System.Collections.Concurrent;
using Enarro.Models.Search;

/// <summary>
/// BM25-based keyword search service
/// </summary>
public class KeywordSearchService : IKeywordSearchService
{
    private readonly ConcurrentDictionary<string, DocumentIndex> _index = new();
    private readonly ILogger<KeywordSearchService> _logger;
    
    // BM25 parameters
    private const double K1 = 1.5;  // Term frequency saturation parameter
    private const double B = 0.75;   // Length normalization parameter
    
    public KeywordSearchService(ILogger<KeywordSearchService> logger)
    {
        _logger = logger;
    }
    
    public Task IndexDocumentAsync(string documentId, string content, CancellationToken ct = default)
    {
        // Tokenize content
        var tokens = Tokenize(content);
        
        // Calculate term frequencies
        var termFrequencies = CalculateTermFrequencies(tokens);
        
        // Store in index
        _index[documentId] = new DocumentIndex
        {
            DocumentId = documentId,
            Content = content,
            TermFrequencies = termFrequencies,
            DocumentLength = tokens.Count
        };
        
        _logger.LogInformation(
            "Indexed document {DocumentId} with {TokenCount} tokens and {UniqueTerms} unique terms",
            documentId, tokens.Count, termFrequencies.Count);
        
        return Task.CompletedTask;
    }
    
    public Task<List<HybridSearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new List<HybridSearchResult>());
        }
        
        var queryTokens = Tokenize(query);
        var scores = new List<(string DocumentId, double Score)>();
        
        // Calculate BM25 scores for all documents
        foreach (var (docId, docIndex) in _index)
        {
            var score = CalculateBM25Score(queryTokens, docIndex);
            if (score > 0)
            {
                scores.Add((docId, score));
            }
        }
        
        // Sort by score and take top K
        var results = scores
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new HybridSearchResult
            {
                DocumentId = x.DocumentId,
                Score = x.Score,
                Content = _index[x.DocumentId].Content,
                Metadata = new Dictionary<string, object>
                {
                    ["searchType"] = "keyword",
                    ["algorithm"] = "BM25",
                    ["queryTerms"] = queryTokens.Count
                }
            })
            .ToList();
        
        _logger.LogInformation(
            "Keyword search for '{Query}' returned {ResultCount} results from {TotalDocs} documents",
            query, results.Count, _index.Count);
        
        return Task.FromResult(results);
    }
    
    public Task RemoveDocumentAsync(string documentId, CancellationToken ct = default)
    {
        if (_index.TryRemove(documentId, out _))
        {
            _logger.LogInformation("Removed document {DocumentId} from keyword index", documentId);
        }
        
        return Task.CompletedTask;
    }
    
    public Task<SearchStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var allTerms = _index.Values
            .SelectMany(d => d.TermFrequencies.Keys)
            .Distinct()
            .Count();
        
        var avgLength = _index.Values.Any() 
            ? _index.Values.Average(d => d.DocumentLength) 
            : 0;
        
        var stats = new SearchStatistics
        {
            TotalDocuments = _index.Count,
            TotalTerms = allTerms,
            AverageDocumentLength = avgLength,
            LastIndexedAt = DateTime.UtcNow
        };
        
        return Task.FromResult(stats);
    }
    
    public Task ClearIndexAsync(CancellationToken ct = default)
    {
        _index.Clear();
        _logger.LogInformation("Cleared keyword search index");
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Calculate BM25 score for a document given query terms
    /// </summary>
    private double CalculateBM25Score(List<string> queryTokens, DocumentIndex docIndex)
    {
        var avgDocLength = _index.Values.Average(d => d.DocumentLength);
        var score = 0.0;
        
        foreach (var term in queryTokens.Distinct())
        {
            if (!docIndex.TermFrequencies.ContainsKey(term))
                continue;
            
            // Term frequency in document
            var tf = docIndex.TermFrequencies[term];
            
            // Document frequency (number of documents containing the term)
            var df = _index.Values.Count(d => d.TermFrequencies.ContainsKey(term));
            
            // Inverse document frequency
            var idf = Math.Log((double)(_index.Count - df + 0.5) / (df + 0.5) + 1.0);
            
            // BM25 formula
            var numerator = tf * (K1 + 1);
            var denominator = tf + K1 * (1 - B + B * (docIndex.DocumentLength / avgDocLength));
            
            score += idf * (numerator / denominator);
        }
        
        return score;
    }
    
    /// <summary>
    /// Tokenize text into terms
    /// </summary>
    private List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();
        
        // Simple tokenization - split on whitespace and punctuation
        // Convert to lowercase for case-insensitive matching
        return text
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' }, 
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1) // Filter out single characters
            .ToList();
    }
    
    /// <summary>
    /// Calculate term frequencies for a list of tokens
    /// </summary>
    private Dictionary<string, int> CalculateTermFrequencies(List<string> tokens)
    {
        return tokens
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

/// <summary>
/// Internal document index structure
/// </summary>
internal class DocumentIndex
{
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, int> TermFrequencies { get; set; } = new();
    public int DocumentLength { get; set; }
}
