namespace Enarro.Models.Search;

/// <summary>
/// Options for hybrid search combining vector and keyword search
/// </summary>
public class HybridSearchOptions
{
    /// <summary>
    /// Weight for vector search results (0-1)
    /// </summary>
    public double VectorWeight { get; set; } = 0.7;
    
    /// <summary>
    /// Weight for keyword search results (0-1)
    /// </summary>
    public double KeywordWeight { get; set; } = 0.3;
    
    /// <summary>
    /// Strategy for ranking combined results
    /// </summary>
    public RankingStrategy RankingStrategy { get; set; } = RankingStrategy.ReciprocalRankFusion;
    
    /// <summary>
    /// Number of top results to return
    /// </summary>
    public int TopK { get; set; } = 10;
    
    /// <summary>
    /// Enable re-ranking of results
    /// </summary>
    public bool EnableReRanking { get; set; } = false;
}

/// <summary>
/// Strategy for ranking search results
/// </summary>
public enum RankingStrategy
{
    /// <summary>
    /// Reciprocal Rank Fusion - combines rankings from multiple sources
    /// </summary>
    ReciprocalRankFusion,
    
    /// <summary>
    /// Weighted score combination
    /// </summary>
    WeightedScore,
    
    /// <summary>
    /// Simple linear combination of scores
    /// </summary>
    LinearCombination
}
