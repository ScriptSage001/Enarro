namespace Enarro.Services.Search;

using Enarro.Models.Search;
using Microsoft.KernelMemory;

/// <summary>
/// Hybrid search service combining vector and keyword search
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly IKernelMemory _memory;
    private readonly IKeywordSearchService _keywordSearch;
    private readonly ILogger<HybridSearchService> _logger;
    
    public HybridSearchService(
        IKernelMemory memory,
        IKeywordSearchService keywordSearch,
        ILogger<HybridSearchService> logger)
    {
        _memory = memory;
        _keywordSearch = keywordSearch;
        _logger = logger;
    }
    
    public async Task<List<HybridSearchResult>> SearchAsync(
        string query,
        HybridSearchOptions options,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Performing hybrid search for query: '{Query}' with strategy: {Strategy}",
            query, options.RankingStrategy);
        
        // Parallel search
        var vectorTask = VectorSearchAsync(query, options.TopK, ct);
        var keywordTask = _keywordSearch.SearchAsync(query, options.TopK, ct);
        
        await Task.WhenAll(vectorTask, keywordTask);
        
        var vectorResults = vectorTask.Result;
        var keywordResults = keywordTask.Result;
        
        _logger.LogInformation(
            "Vector search returned {VectorCount} results, keyword search returned {KeywordCount} results",
            vectorResults.Count, keywordResults.Count);
        
        // Merge results based on strategy
        var mergedResults = options.RankingStrategy switch
        {
            RankingStrategy.ReciprocalRankFusion => 
                MergeWithRRF(vectorResults, keywordResults),
            RankingStrategy.WeightedScore => 
                MergeWithWeightedScore(vectorResults, keywordResults, options),
            RankingStrategy.LinearCombination => 
                MergeWithLinearCombination(vectorResults, keywordResults, options),
            _ => throw new ArgumentException($"Unknown ranking strategy: {options.RankingStrategy}")
        };
        
        _logger.LogInformation(
            "Hybrid search returned {Count} merged results",
            mergedResults.Count);
        
        return mergedResults;
    }
    
    /// <summary>
    /// Perform vector search using Kernel Memory
    /// </summary>
    private async Task<List<HybridSearchResult>> VectorSearchAsync(
        string query,
        int topK,
        CancellationToken ct)
    {
        var searchResult = await _memory.SearchAsync(
            query,
            limit: topK,
            cancellationToken: ct);
        
        return searchResult.Results
            .SelectMany(r => r.Partitions.Select(p => new HybridSearchResult
            {
                DocumentId = r.DocumentId,
                Score = p.Relevance,
                Content = p.Text,
                Metadata = new Dictionary<string, object>
                {
                    ["searchType"] = "vector",
                    ["relevance"] = p.Relevance,
                    ["partitionNumber"] = p.PartitionNumber
                }
            }))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }
    
    /// <summary>
    /// Merge results using Reciprocal Rank Fusion (RRF)
    /// </summary>
    private List<HybridSearchResult> MergeWithRRF(
        List<HybridSearchResult> vectorResults,
        List<HybridSearchResult> keywordResults,
        int k = 60)
    {
        var rrfScores = new Dictionary<string, double>();
        
        // Calculate RRF scores for vector results
        for (int i = 0; i < vectorResults.Count; i++)
        {
            var docId = vectorResults[i].DocumentId;
            rrfScores[docId] = rrfScores.GetValueOrDefault(docId) + 1.0 / (k + i + 1);
        }
        
        // Calculate RRF scores for keyword results
        for (int i = 0; i < keywordResults.Count; i++)
        {
            var docId = keywordResults[i].DocumentId;
            rrfScores[docId] = rrfScores.GetValueOrDefault(docId) + 1.0 / (k + i + 1);
        }
        
        // Combine all unique results
        var allResults = vectorResults
            .Concat(keywordResults)
            .GroupBy(r => r.DocumentId)
            .Select(g => g.First())
            .ToList();
        
        // Sort by RRF score and update metadata
        return allResults
            .OrderByDescending(r => rrfScores[r.DocumentId])
            .Select(r =>
            {
                r.Score = rrfScores[r.DocumentId];
                r.Metadata["rankingStrategy"] = "RRF";
                r.Metadata["rrfScore"] = rrfScores[r.DocumentId];
                return r;
            })
            .ToList();
    }
    
    /// <summary>
    /// Merge results using weighted score combination
    /// </summary>
    private List<HybridSearchResult> MergeWithWeightedScore(
        List<HybridSearchResult> vectorResults,
        List<HybridSearchResult> keywordResults,
        HybridSearchOptions options)
    {
        var combinedScores = new Dictionary<string, (HybridSearchResult Result, double Score)>();
        
        // Normalize and weight vector scores
        if (vectorResults.Any())
        {
            var maxVectorScore = vectorResults.Max(r => r.Score);
            if (maxVectorScore > 0)
            {
                foreach (var result in vectorResults)
                {
                    var normalizedScore = result.Score / maxVectorScore;
                    var weightedScore = normalizedScore * options.VectorWeight;
                    combinedScores[result.DocumentId] = (result, weightedScore);
                }
            }
        }
        
        // Normalize and weight keyword scores
        if (keywordResults.Any())
        {
            var maxKeywordScore = keywordResults.Max(r => r.Score);
            if (maxKeywordScore > 0)
            {
                foreach (var result in keywordResults)
                {
                    var normalizedScore = result.Score / maxKeywordScore;
                    var weightedScore = normalizedScore * options.KeywordWeight;
                    
                    if (combinedScores.ContainsKey(result.DocumentId))
                    {
                        var existing = combinedScores[result.DocumentId];
                        combinedScores[result.DocumentId] = (existing.Result, existing.Score + weightedScore);
                    }
                    else
                    {
                        combinedScores[result.DocumentId] = (result, weightedScore);
                    }
                }
            }
        }
        
        return combinedScores.Values
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                x.Result.Score = x.Score;
                x.Result.Metadata["rankingStrategy"] = "WeightedScore";
                x.Result.Metadata["vectorWeight"] = options.VectorWeight;
                x.Result.Metadata["keywordWeight"] = options.KeywordWeight;
                return x.Result;
            })
            .ToList();
    }
    
    /// <summary>
    /// Merge results using linear combination
    /// </summary>
    private List<HybridSearchResult> MergeWithLinearCombination(
        List<HybridSearchResult> vectorResults,
        List<HybridSearchResult> keywordResults,
        HybridSearchOptions options)
    {
        var combinedScores = new Dictionary<string, (HybridSearchResult Result, double Score)>();
        
        // Add vector scores
        foreach (var result in vectorResults)
        {
            var weightedScore = result.Score * options.VectorWeight;
            combinedScores[result.DocumentId] = (result, weightedScore);
        }
        
        // Add keyword scores
        foreach (var result in keywordResults)
        {
            var weightedScore = result.Score * options.KeywordWeight;
            
            if (combinedScores.ContainsKey(result.DocumentId))
            {
                var existing = combinedScores[result.DocumentId];
                combinedScores[result.DocumentId] = (existing.Result, existing.Score + weightedScore);
            }
            else
            {
                combinedScores[result.DocumentId] = (result, weightedScore);
            }
        }
        
        return combinedScores.Values
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                x.Result.Score = x.Score;
                x.Result.Metadata["rankingStrategy"] = "LinearCombination";
                return x.Result;
            })
            .ToList();
    }
}
