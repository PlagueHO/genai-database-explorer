using GenAIDBExplorer.Core.Repository.DTO;

namespace GenAIDBExplorer.Core.Repository.Mappers;

/// <summary>
/// Default mapper for LocalDisk and AzureBlob strategies that merges embedding payload into entity JSON.
/// Note: We return object to allow existing serializers to handle domain types; we compose with an envelope when embedding exists.
/// </summary>
public sealed class LocalBlobEntityMapper : IStorageEntityMapper
{
    public object ToPersistedEntity(object entity, EmbeddingPayload? embedding)
    {
        if (embedding == null)
        {
            return entity;
        }

        // Create a typed envelope: { data: entity, embedding: { vector, metadata } }
        return new PersistedEntityDto
        {
            Data = entity,
            Embedding = embedding
        };
    }

    public CosmosEntityDto<T> ToCosmosEntity<T>(string modelName, string entityType, string entityName, T entity, EmbeddingMetadata? metadata)
    {
        return new CosmosEntityDto<T>
        {
            Id = $"{modelName}_{entityType}_{entityName}",
            ModelName = modelName,
            EntityType = entityType,
            EntityName = entityName,
            Data = entity,
            Embedding = metadata,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
