using System;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.VectorEmbedding.Models;

/// <summary>
/// Immutable configuration record for vector repository operations.
/// Uses record type with init properties for thread-safe immutable configuration.
/// </summary>
public record VectorRepositoryOptions
{
    /// <summary>
    /// Gets the embedding model to use for generating vectors.
    /// </summary>
    /// <remarks>
    /// Supported models:
    /// - text-embedding-ada-002: 1536 dimensions (default)
    /// - text-embedding-3-small: 1536 dimensions
    /// - text-embedding-3-large: 3072 dimensions
    /// </remarks>
    public string EmbeddingModel { get; init; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets the vector dimensions for the embedding model.
    /// </summary>
    /// <remarks>
    /// Must match the embedding model capabilities:
    /// - 1536 for ada-002 and 3-small models
    /// - 3072 for 3-large model
    /// </remarks>
    public int Dimensions { get; init; } = 1536;

    /// <summary>
    /// Gets the types of entities to include in vector generation.
    /// </summary>
    public EntityTypes EntityTypes { get; init; } = EntityTypes.All;

    /// <summary>
    /// Gets a value indicating whether to regenerate existing vector embeddings.
    /// </summary>
    /// <remarks>
    /// When false, only entities without embeddings or with stale embeddings will be processed.
    /// When true, all specified entities will have their embeddings regenerated.
    /// </remarks>
    public bool RegenerateExisting { get; init; } = false;

    /// <summary>
    /// Gets the batch size for processing embeddings to manage API rate limits.
    /// </summary>
    public int BatchSize { get; init; } = 10;

    /// <summary>
    /// Gets the maximum degree of parallelism for concurrent operations.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>
    /// Gets the timeout for vector generation operations.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets the name of the persistence strategy to use for vector storage.
    /// </summary>
    public string? StrategyName { get; init; }
}

/// <summary>
/// Immutable configuration record for vector search operations.
/// </summary>
public record VectorSearchOptions
{
    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>
    /// Gets the minimum similarity threshold for including results.
    /// </summary>
    /// <remarks>
    /// Cosine similarity score between 0.0 and 1.0.
    /// Higher values return only more similar results.
    /// Typical values: 0.7-0.8 for good matches, 0.5-0.7 for broader search.
    /// </remarks>
    public double MinimumSimilarity { get; init; } = 0.5;

    /// <summary>
    /// Gets the types of entities to include in the search.
    /// </summary>
    public EntityTypes EntityTypes { get; init; } = EntityTypes.All;

    /// <summary>
    /// Gets the embedding model to use for query vectorization.
    /// </summary>
    /// <remarks>
    /// Must match the model used for stored embeddings to ensure accurate similarity comparison.
    /// </remarks>
    public string EmbeddingModel { get; init; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets a value indicating whether to include embedding vectors in search results.
    /// </summary>
    /// <remarks>
    /// Setting to false reduces memory usage and network transfer for large result sets.
    /// Embeddings are typically only needed for further processing or analysis.
    /// </remarks>
    public bool IncludeEmbeddings { get; init; } = false;

    /// <summary>
    /// Gets the name of the persistence strategy to use for vector retrieval.
    /// </summary>
    public string? StrategyName { get; init; }

    /// <summary>
    /// Gets the timeout for search operations.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Flags enumeration for specifying which entity types to process.
/// </summary>
[Flags]
public enum EntityTypes
{
    /// <summary>No entity types selected.</summary>
    None = 0,

    /// <summary>Include tables in operations.</summary>
    Tables = 1,

    /// <summary>Include views in operations.</summary>
    Views = 2,

    /// <summary>Include stored procedures in operations.</summary>
    StoredProcedures = 4,

    /// <summary>Include all entity types in operations.</summary>
    All = Tables | Views | StoredProcedures
}
