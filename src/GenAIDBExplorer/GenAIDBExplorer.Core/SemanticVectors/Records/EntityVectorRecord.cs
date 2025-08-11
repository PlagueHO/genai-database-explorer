using Microsoft.Extensions.VectorData;

namespace GenAIDBExplorer.Core.SemanticVectors.Records;

/// <summary>
/// Vector record representing a semantic model entity with metadata for search and indexing.
/// </summary>
public sealed class EntityVectorRecord
{
    [VectorStoreKey]
    public required string Id { get; init; }

    [VectorStoreData]
    public required string Content { get; init; }

    // InMemory connector requires a known vector field; default expected dimension is 3072 (configurable at runtime for other providers).
    [VectorStoreVector(3072)]
    public ReadOnlyMemory<float> Vector { get; init; }

    [VectorStoreData]
    public string? Schema { get; init; }

    [VectorStoreData]
    public string? EntityType { get; init; }

    [VectorStoreData]
    public string? Name { get; init; }

    [VectorStoreData]
    public string? ContentHash { get; init; }
}
