using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GenAIDBExplorer.Api.Health;

/// <summary>
/// Reports the health of the semantic model cache.
/// Healthy when the model is loaded, Unhealthy when not loaded.
/// </summary>
public class SemanticModelHealthCheck(
    Services.ISemanticModelCacheService cacheService
) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (cacheService.IsLoaded)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Semantic model is loaded."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Semantic model is not loaded."));
    }
}
