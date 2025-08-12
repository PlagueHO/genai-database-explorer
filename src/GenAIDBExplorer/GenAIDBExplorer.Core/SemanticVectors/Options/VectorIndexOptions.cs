using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.SemanticVectors.Options;

/// <summary>
/// Options for configuring vector indexing providers and behavior.
/// </summary>
public sealed class VectorIndexOptions
{
    public const string SectionName = "VectorIndex";

    /// <summary>
    /// Provider for vector indexing. Valid values: Auto, InMemory, AzureAISearch, CosmosDB.
    /// </summary>
    [Required]
    public string Provider { get; init; } = "Auto";

    /// <summary>
    /// Name of the collection or index to store vectors in.
    /// </summary>
    [Required]
    public string CollectionName { get; init; } = "genaide-entities";

    /// <summary>
    /// When true, will push new/updated vectors to the external index during generation.
    /// </summary>
    public bool PushOnGenerate { get; init; } = true;

    /// <summary>
    /// When true and the provider supports it, attempt to provision the index if missing.
    /// </summary>
    public bool ProvisionIfMissing { get; init; } = false;

    /// <summary>
    /// Restrict providers that can be used for the current repository strategy. Example: ["LocalDisk","AzureBlob","Cosmos"].
    /// </summary>
    public string[] AllowedForRepository { get; init; } = [];

    /// <summary>
    /// Azure AI Search specific options.
    /// </summary>
    public AzureAISearchOptions AzureAISearch { get; init; } = new();

    /// <summary>
    /// CosmosDB same-container specific options.
    /// </summary>
    public CosmosDBOptions CosmosDB { get; init; } = new();

    /// <summary>
    /// Legacy Cosmos NoSQL options. Deprecated in favor of CosmosDB.
    /// </summary>
    [Obsolete("Use CosmosDB with VectorPath/DistanceFunction/IndexType. This will be removed in a future version.")]
    public CosmosNoSqlOptions CosmosNoSql { get; init; } = new();

    /// <summary>
    /// Service ID to use for the embedding generator in the Semantic Kernel.
    /// </summary>
    [Required]
    public string EmbeddingServiceId { get; init; } = "Embeddings";

    /// <summary>
    /// Expected vector dimensionality; validated against actual embedding model output.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? ExpectedDimensions { get; init; }

    /// <summary>
    /// Hybrid search settings when supported by provider.
    /// </summary>
    public HybridSearchOptions Hybrid { get; init; } = new();

    public sealed class AzureAISearchOptions
    {
        public string? Endpoint { get; init; }
        public string? IndexName { get; init; }
        public string? ApiKey { get; init; }
    }

    /// <summary>
    /// CosmosDB vector options for same-container storage. These settings control
    /// the vector field and index behavior on the Entities container.
    /// </summary>
    public sealed class CosmosDBOptions
    {
        /// <summary>
        /// Path to the vector field on the entity document, e.g. "/embeddings/title".
        /// </summary>
        public string? VectorPath { get; init; }

        /// <summary>
        /// Distance function to use. Allowed values: cosine, dotproduct, euclidean.
        /// </summary>
        public string? DistanceFunction { get; init; }

        /// <summary>
        /// Index type to use. Allowed values: diskANN, quantizedFlat, flat.
        /// </summary>
        public string? IndexType { get; init; }
    }

    /// <summary>
    /// Legacy Cosmos NoSQL options kept for backward compatibility during transition.
    /// </summary>
    [Obsolete("Use CosmosDB options. This legacy options class will be removed in a future version.")]
    public sealed class CosmosNoSqlOptions
    {
        public string? AccountEndpoint { get; init; }
        public string? Database { get; init; }
        public string? Container { get; init; }
    }

    public sealed class HybridSearchOptions
    {
        public bool Enabled { get; init; }
        public double? TextWeight { get; init; }
        public double? VectorWeight { get; init; }
    }
}
