using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Repository.DTO;

/// <summary>
/// Metadata about an embedding vector persisted alongside entities.
/// </summary>
public sealed class EmbeddingMetadata
{
    [JsonPropertyName("modelId")] public string? ModelId { get; set; }
    [JsonPropertyName("dimensions")] public int? Dimensions { get; set; }
    [JsonPropertyName("contentHash")] public string? ContentHash { get; set; }
    [JsonPropertyName("generatedAt")] public DateTimeOffset? GeneratedAt { get; set; }
    [JsonPropertyName("serviceId")] public string? ServiceId { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
}

/// <summary>
/// Embedding payload persisted for Local/Blob strategies.
/// </summary>
public sealed class EmbeddingPayload
{
    /// <summary>
    /// The vector floats. For Local/Blob this is persisted as human-readable JSON.
    /// For Cosmos DB, vectors are not stored (see CosmosDbEntityDto). May be null when not generated.
    /// </summary>
    [JsonPropertyName("vector")] public float[]? Vector { get; set; }

    /// <summary>
    /// Additional metadata describing the embedding generation.
    /// </summary>
    [JsonPropertyName("metadata")] public EmbeddingMetadata? Metadata { get; set; }
}

/// <summary>
/// Envelope persisted for Local/Blob strategies combining entity data with optional embedding.
/// Using object for Data preserves serializer compatibility with existing domain converters.
/// Includes version information for schema evolution.
/// </summary>
public sealed class PersistedEntityDto
{
    /// <summary>
    /// Schema version for this persisted entity format.
    /// Version 1: Original envelope format with data + optional embedding.
    /// </summary>
    [JsonPropertyName("version")] public int Version { get; set; } = 1;

    [JsonPropertyName("data")] public object? Data { get; set; }

    [JsonPropertyName("embedding")] public EmbeddingPayload? Embedding { get; set; }
}
