namespace Enarro.Services.Search;

using Enarro.Models.Search;

/// <summary>
/// Service for hybrid search combining vector and keyword search
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Perform hybrid search combining vector and keyword search
    /// </summary>
    Task<List<HybridSearchResult>> SearchAsync(
        string query,
        HybridSearchOptions options,
        CancellationToken ct = default);
}
