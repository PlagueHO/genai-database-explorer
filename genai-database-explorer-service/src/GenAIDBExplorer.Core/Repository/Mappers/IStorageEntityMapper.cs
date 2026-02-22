using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.DTO;

namespace GenAIDBExplorer.Core.Repository.Mappers;

/// <summary>
/// Maps between domain entities and storage DTOs, handling provider-specific embedding persistence.
/// </summary>
public interface IStorageEntityMapper
{
    // Local/Blob: include floats
    object ToPersistedEntity(object entity, EmbeddingPayload? embedding);

    // Cosmos: metadata only
    CosmosDbEntityDto<T> ToCosmosDbEntity<T>(string modelName, string entityType, string entityName, T entity, EmbeddingMetadata? metadata);
}
