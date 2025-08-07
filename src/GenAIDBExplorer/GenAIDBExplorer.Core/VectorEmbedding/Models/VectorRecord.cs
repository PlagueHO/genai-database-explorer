using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.VectorEmbedding.Models;

/// <summary>
/// Vector record containing entity data and embedding for Semantic Kernel Vector Store.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
/// <remarks>
/// Uses Semantic Kernel Vector Store annotations as defined in:
/// https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model
/// 
/// Supports configurable vector dimensions for different embedding models:
/// - text-embedding-ada-002: 1536 dimensions (default)
/// - text-embedding-3-small: 1536 dimensions  
/// - text-embedding-3-large: 3072 dimensions
/// </remarks>
public sealed class VectorRecord<TEntity> where TEntity : SemanticModelEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the vector record.
    /// </summary>
    /// <remarks>
    /// Composite key format: "{EntityType}_{Schema}_{Name}"
    /// Example: "SemanticModelTable_dbo_Users"
    /// </remarks>
    [VectorStoreKey]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic model entity this vector represents.
    /// </summary>
    /// <remarks>
    /// Contains the full entity data including schema, name, description,
    /// semantic description, and entity-specific properties.
    /// </remarks>
    [VectorStoreData]
    [JsonPropertyName("entity")]
    public TEntity Entity { get; set; } = default!;

    /// <summary>
    /// Gets or sets the aggregated text content used for embedding generation.
    /// </summary>
    /// <remarks>
    /// Combines entity metadata into searchable text:
    /// - Entity name and schema
    /// - Description and semantic description
    /// - Column names and descriptions (for tables/views)
    /// - Parameters and definition (for stored procedures)
    /// </remarks>
    [VectorStoreData]
    [JsonPropertyName("searchableText")]
    public string SearchableText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vector embedding for the entity.
    /// </summary>
    /// <remarks>
    /// Vector dimensions depend on the embedding model used:
    /// - 1536 dimensions for ada-002 and 3-small models
    /// - 3072 dimensions for 3-large model
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Gets or sets the embedding model used to generate the vector.
    /// </summary>
    /// <remarks>
    /// Stored for validation and compatibility checking during search operations.
    /// Must match the model used for query embedding to ensure accurate similarity.
    /// </remarks>
    [VectorStoreData]
    [JsonPropertyName("embeddingModel")]
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets or sets the number of dimensions in the embedding vector.
    /// </summary>
    /// <remarks>
    /// Stored to validate compatibility with the vector store configuration
    /// and to support migration between different embedding models.
    /// </remarks>
    [VectorStoreData]
    [JsonPropertyName("dimensions")]
    public int Dimensions { get; set; } = 1536;

    /// <summary>
    /// Gets or sets the timestamp when the vector was created.
    /// </summary>
    [VectorStoreData]
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the vector was last updated.
    /// </summary>
    [VectorStoreData]
    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the version of the semantic description when the vector was generated.
    /// </summary>
    /// <remarks>
    /// Used to detect when entities have been enriched with new semantic descriptions
    /// and need their vectors regenerated for optimal search accuracy.
    /// </remarks>
    [VectorStoreData]
    [JsonPropertyName("semanticDescriptionVersion")]
    public DateTimeOffset? SemanticDescriptionVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the VectorRecord class.
    /// </summary>
    public VectorRecord()
    {
    }

    /// <summary>
    /// Initializes a new instance of the VectorRecord class with the specified entity.
    /// </summary>
    /// <param name="entity">The semantic model entity.</param>
    /// <param name="searchableText">The aggregated searchable text content.</param>
    /// <param name="embeddingModel">The embedding model used.</param>
    /// <param name="dimensions">The number of vector dimensions.</param>
    public VectorRecord(TEntity entity, string searchableText, string embeddingModel = "text-embedding-ada-002", int dimensions = 1536)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        SearchableText = searchableText ?? throw new ArgumentNullException(nameof(searchableText));
        EmbeddingModel = embeddingModel;
        Dimensions = dimensions;
        Id = GenerateId(entity);
        SemanticDescriptionVersion = entity.SemanticDescriptionLastUpdate;
    }

    /// <summary>
    /// Generates a unique identifier for the vector record based on the entity.
    /// </summary>
    /// <param name="entity">The semantic model entity.</param>
    /// <returns>A unique identifier string.</returns>
    public static string GenerateId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = entity.GetType().Name;
        return $"{entityType}_{entity.Schema}_{entity.Name}";
    }

    /// <summary>
    /// Updates the LastUpdated timestamp to the current UTC time.
    /// </summary>
    public void MarkAsUpdated()
    {
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if the vector needs to be regenerated based on semantic description changes.
    /// </summary>
    /// <returns>True if the vector should be regenerated; otherwise, false.</returns>
    public bool NeedsRegeneration()
    {
        // Regenerate if entity has newer semantic description than vector
        return Entity.SemanticDescriptionLastUpdate.HasValue &&
               (!SemanticDescriptionVersion.HasValue ||
                Entity.SemanticDescriptionLastUpdate > SemanticDescriptionVersion);
    }
}
