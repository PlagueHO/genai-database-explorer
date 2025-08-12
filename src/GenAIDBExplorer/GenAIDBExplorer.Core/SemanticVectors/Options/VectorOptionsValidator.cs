using GenAIDBExplorer.Core.SemanticVectors.Options;
using Microsoft.Extensions.Options;

namespace GenAIDBExplorer.Core.SemanticVectors.Options;

/// <summary>
/// Validates <see cref="VectorIndexOptions"/> for correctness at startup.
/// </summary>
public sealed class VectorOptionsValidator : IValidateOptions<VectorIndexOptions>
{
    public ValidateOptionsResult Validate(string? name, VectorIndexOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("VectorIndex options are required.");
        }

        var errors = new List<string>();

        // Provider validation
        var provider = options.Provider?.Trim();
        var validProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Auto", "InMemory", "AzureAISearch", "CosmosDB"
        };
        if (string.IsNullOrWhiteSpace(provider) || !validProviders.Contains(provider))
        {
            errors.Add($"Provider must be one of: {string.Join(", ", validProviders)}.");
        }

        // Basic required fields
        if (string.IsNullOrWhiteSpace(options.CollectionName))
        {
            errors.Add("CollectionName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EmbeddingServiceId))
        {
            errors.Add("EmbeddingServiceId is required.");
        }

        // ExpectedDimensions should be positive when provided
        if (options.ExpectedDimensions is int d && d <= 0)
        {
            errors.Add("ExpectedDimensions must be a positive integer when specified.");
        }

        // Hybrid weights sanity check
        if (options.Hybrid is { Enabled: true } && options.Hybrid.TextWeight is double tw && options.Hybrid.VectorWeight is double vw)
        {
            if (tw < 0 || vw < 0)
            {
                errors.Add("Hybrid.TextWeight and Hybrid.VectorWeight must be non-negative.");
            }
        }

        // Provider-specific minimal checks
        if (provider?.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrWhiteSpace(options.AzureAISearch?.Endpoint))
            {
                errors.Add("AzureAISearch.Endpoint is required when Provider=AzureAISearch.");
            }
            if (string.IsNullOrWhiteSpace(options.AzureAISearch?.IndexName))
            {
                errors.Add("AzureAISearch.IndexName is required when Provider=AzureAISearch.");
            }
        }

        if (provider?.Equals("CosmosDB", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrWhiteSpace(options.CosmosDB?.VectorPath))
            {
                errors.Add("CosmosDB.VectorPath is required when Provider=CosmosDB.");
            }

            var distance = options.CosmosDB?.DistanceFunction?.Trim();
            if (!string.IsNullOrWhiteSpace(distance))
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cosine", "dotproduct", "euclidean" };
                if (!allowed.Contains(distance))
                {
                    errors.Add("CosmosDB.DistanceFunction must be one of: cosine, dotproduct, euclidean.");
                }
            }

            var indexType = options.CosmosDB?.IndexType?.Trim();
            if (!string.IsNullOrWhiteSpace(indexType))
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "diskANN", "quantizedFlat", "flat" };
                if (!allowed.Contains(indexType))
                {
                    errors.Add("CosmosDB.IndexType must be one of: diskANN, quantizedFlat, flat.");
                }
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
