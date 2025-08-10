using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Repository.DTO;

/// <summary>
/// Cosmos-specific entity DTO that stores only metadata about embeddings, not the raw vectors.
/// </summary>
/// <typeparam name="T">Domain entity type (e.g., SemanticModelTable)</typeparam>
public sealed class CosmosEntityDto<T>
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("modelName")] public string ModelName { get; set; } = string.Empty;
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("entityName")] public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// The domain entity data without the vector floats.
    /// </summary>
    [JsonPropertyName("data")] public T? Data { get; set; }

    /// <summary>
    /// Optional embedding metadata (no vectors).
    /// </summary>
    [JsonPropertyName("embedding")] public EmbeddingMetadata? Embedding { get; set; }

    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
