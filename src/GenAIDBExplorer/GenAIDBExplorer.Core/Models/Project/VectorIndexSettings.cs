using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Project-scoped configuration for vector indexing and embeddings.
/// </summary>
public sealed class VectorIndexSettings
{
    public const string PropertyName = "VectorIndex";

    [Required, NotEmptyOrWhitespace]
    public string Provider { get; set; } = "Auto"; // Auto, InMemory, AzureAISearch, CosmosNoSql

    [Required, NotEmptyOrWhitespace]
    public string CollectionName { get; set; } = "genaide-entities";

    public bool PushOnGenerate { get; set; } = true;
    public bool ProvisionIfMissing { get; set; }

    [Required, NotEmptyOrWhitespace]
    public string EmbeddingServiceId { get; set; } = "Embeddings";

    [Range(1, int.MaxValue)]
    public int? ExpectedDimensions { get; set; }

    public string[] AllowedForRepository { get; set; } = [];

    public AzureAISearchSettings AzureAISearch { get; set; } = new();
    public CosmosNoSqlSettings CosmosNoSql { get; set; } = new();
    public HybridSearchSettings Hybrid { get; set; } = new();

    public sealed class AzureAISearchSettings
    {
        public string? Endpoint { get; set; }
        public string? IndexName { get; set; }
        public string? ApiKey { get; set; }
    }

    public sealed class CosmosNoSqlSettings
    {
        public string? AccountEndpoint { get; set; }
        public string? Database { get; set; }
        public string? Container { get; set; }
    }

    public sealed class HybridSearchSettings
    {
        public bool Enabled { get; set; }
        public double? TextWeight { get; set; }
        public double? VectorWeight { get; set; }
    }
}
