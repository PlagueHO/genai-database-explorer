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
            "Auto", "InMemory", "AzureAISearch", "CosmosNoSql"
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

        if (provider?.Equals("CosmosNoSql", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrWhiteSpace(options.CosmosNoSql?.AccountEndpoint))
            {
                errors.Add("CosmosNoSql.AccountEndpoint is required when Provider=CosmosNoSql.");
            }
            if (string.IsNullOrWhiteSpace(options.CosmosNoSql?.Database) || string.IsNullOrWhiteSpace(options.CosmosNoSql?.Container))
            {
                errors.Add("CosmosNoSql.Database and CosmosNoSql.Container are required when Provider=CosmosNoSql.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
