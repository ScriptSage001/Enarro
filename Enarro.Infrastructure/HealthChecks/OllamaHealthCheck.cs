using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Enarro.Infrastructure.HealthChecks;

/// <summary>
/// Health check for the Ollama LLM service.
/// Uses IHttpClientFactory for proper connection pooling.
/// </summary>
public class OllamaHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var ollamaEndpoint = GetOllamaEndpoint();
            if (string.IsNullOrEmpty(ollamaEndpoint))
            {
                return HealthCheckResult.Unhealthy("Ollama endpoint not configured.");
            }

            using var client = httpClientFactory.CreateClient("OllamaHealthCheck");
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"{ollamaEndpoint}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama is responding.")
                : HealthCheckResult.Unhealthy($"Ollama returned {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ollama is unreachable.", ex);
        }
    }

    private string? GetOllamaEndpoint()
    {
        var cs = configuration.GetConnectionString("ollama");
        if (string.IsNullOrEmpty(cs)) return null;

        var csBuilder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cs };
        return csBuilder.TryGetValue("Endpoint", out var url) ? (string)url : null;
    }
}
