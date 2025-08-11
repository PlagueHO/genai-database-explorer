using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.SemanticVectors.Policy;

public class VectorIndexPolicy : IVectorIndexPolicy
{
    public string ResolveProvider(VectorIndexSettings settings, string repositoryStrategy)
    {
        ArgumentNullException.ThrowIfNull(settings);
        repositoryStrategy = repositoryStrategy?.Trim() ?? string.Empty;

        if (!string.Equals(settings.Provider, "Auto", StringComparison.OrdinalIgnoreCase))
            return settings.Provider;

        // Auto rules:
        // - If repository is Cosmos, require CosmosNoSql (CON-002)
        // - Otherwise default to InMemory for local development
        if (repositoryStrategy.Equals("Cosmos", StringComparison.OrdinalIgnoreCase))
        {
            return "CosmosNoSql";
        }

        // Future: detect Azure AI Search availability/env vars and prefer it.
        return "InMemory";
    }

    public void Validate(VectorIndexSettings settings, string repositoryStrategy)
    {
        ArgumentNullException.ThrowIfNull(settings);
        repositoryStrategy = repositoryStrategy?.Trim() ?? string.Empty;

        // Basic compatibility: Cosmos strategy can't use external index simultaneously (CON-002)
        if (repositoryStrategy.Equals("Cosmos", StringComparison.OrdinalIgnoreCase))
        {
            var provider = ResolveProvider(settings, repositoryStrategy);
            if (!string.Equals(provider, "CosmosNoSql", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cosmos persistence requires CosmosNoSql vector provider (CON-002).");
            }
        }

        if (settings.ExpectedDimensions.HasValue && settings.ExpectedDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.ExpectedDimensions), "ExpectedDimensions must be > 0.");
        }

        // If AllowedForRepository constraint is provided, ensure provider is permitted for the current repo strategy
        if (settings.AllowedForRepository is { Length: > 0 })
        {
            var allowed = new HashSet<string>(settings.AllowedForRepository, StringComparer.OrdinalIgnoreCase);
            if (!allowed.Contains(repositoryStrategy))
            {
                throw new InvalidOperationException($"Vector provider '{settings.Provider}' is not allowed for repository strategy '{repositoryStrategy}'.");
            }
        }
    }
}
