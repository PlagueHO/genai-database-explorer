using GenAIDBExplorer.Core.Repository.DTO;

namespace GenAIDBExplorer.Core.Repository.Mappers;

/// <summary>
/// Mapper for Cosmos DB strategy; ensures vectors are not included in documents.
/// </summary>
public sealed class CosmosDbEntityMapper : IStorageEntityMapper
{
    public object ToPersistedEntity(object entity, EmbeddingPayload? embedding)
    {
        // For Cosmos DB, we never persist vector floats in the entity document
        return entity; // caller should use ToCosmosDbEntity
    }

    public CosmosDbEntityDto<T> ToCosmosDbEntity<T>(string modelName, string entityType, string entityName, T entity, EmbeddingMetadata? metadata)
    {
        return new CosmosDbEntityDto<T>
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
