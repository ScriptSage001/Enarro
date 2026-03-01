using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qdrant.Client;

namespace Enarro.Infrastructure.HealthChecks;

/// <summary>
/// Health check for the Qdrant vector database.
/// Properly async — no more .Result blocking calls.
/// </summary>
public class QdrantHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var qdrantEndpoint = GetQdrantEndpoint();
            if (string.IsNullOrEmpty(qdrantEndpoint))
            {
                return HealthCheckResult.Unhealthy("Qdrant endpoint not configured.");
            }

            var apiKey = configuration.GetValue<string>("QDRANT_APIKEY");
            var url = new Uri(qdrantEndpoint);
            var client = new QdrantClient(url, apiKey);

            await client.ListCollectionsAsync(cancellationToken);
            return HealthCheckResult.Healthy("Qdrant is responding.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant is unreachable.", ex);
        }
    }

    private string? GetQdrantEndpoint()
    {
        var cs = configuration.GetConnectionString("qdrant");
        if (string.IsNullOrEmpty(cs)) return null;

        var csBuilder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cs };
        return csBuilder.TryGetValue("Endpoint", out var url) ? (string)url : null;
    }
}
