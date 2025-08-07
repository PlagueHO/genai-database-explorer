using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.VectorEmbedding.Models;

namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Persistence strategy interface for vector embedding operations.
/// </summary>
/// <remarks>
/// This interface extends the repository pattern to handle vector-specific storage
/// requirements while maintaining compatibility with existing persistence strategies.
/// 
/// Each implementation should:
/// - Handle vector-specific storage formats and optimizations
/// - Support efficient similarity search operations
/// - Maintain consistency with semantic model persistence
/// - Provide atomic operations with rollback capabilities
/// - Use the same security and configuration patterns
/// 
/// Implementations may leverage different storage technologies:
/// - LocalDisk: JSON files with in-memory similarity search
/// - AzureBlob: Blob storage with metadata for vector operations
/// - CosmosDB: Native vector search capabilities with indexed embeddings
/// </remarks>
public interface IVectorPersistenceStrategy
{
    /// <summary>
    /// Saves vector embeddings for the specified entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="vectorRecords">The vector records to save.</param>
    /// <param name="modelPath">The path where vectors should be stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the save operation.</returns>
    Task SaveVectorEmbeddingsAsync<TEntity>(
        IEnumerable<VectorRecord<TEntity>> vectorRecords,
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Loads vector embeddings for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of vector records for the specified entity type.</returns>
    Task<IEnumerable<VectorRecord<TEntity>>> LoadVectorEmbeddingsAsync<TEntity>(
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Performs similarity search against stored vector embeddings.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="queryEmbedding">The query vector to search with.</param>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="options">Search configuration options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of search results ranked by similarity.</returns>
    Task<IEnumerable<VectorSearchResult<TEntity>>> SearchSimilarAsync<TEntity>(
        ReadOnlyMemory<float> queryEmbedding,
        DirectoryInfo modelPath,
        VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Checks if vector embeddings exist for the specified model path.
    /// </summary>
    /// <param name="modelPath">The path to check for vector embeddings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if vector embeddings exist; otherwise, false.</returns>
    Task<bool> HasVectorEmbeddingsAsync(
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes vector embeddings for the specified model path.
    /// </summary>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the deletion operation.</returns>
    Task DeleteVectorEmbeddingsAsync(
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates vector embeddings for specific entities, performing an upsert operation.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="vectorRecords">The vector records to update.</param>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the update operation.</returns>
    Task UpdateVectorEmbeddingsAsync<TEntity>(
        IEnumerable<VectorRecord<TEntity>> vectorRecords,
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Gets metadata about stored vector embeddings.
    /// </summary>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Metadata about the stored vectors.</returns>
    Task<VectorStorageMetadata> GetVectorMetadataAsync(
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of stored vector embeddings.
    /// </summary>
    /// <param name="modelPath">The path where vectors are stored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Validation result indicating any issues found.</returns>
    Task<VectorValidationResult> ValidateVectorIntegrityAsync(
        DirectoryInfo modelPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata about stored vector embeddings.
/// </summary>
public record VectorStorageMetadata
{
    /// <summary>
    /// Gets the total number of vector embeddings stored.
    /// </summary>
    public int TotalVectorCount { get; init; }

    /// <summary>
    /// Gets the number of table embeddings stored.
    /// </summary>
    public int TableVectorCount { get; init; }

    /// <summary>
    /// Gets the number of view embeddings stored.
    /// </summary>
    public int ViewVectorCount { get; init; }

    /// <summary>
    /// Gets the number of stored procedure embeddings stored.
    /// </summary>
    public int StoredProcedureVectorCount { get; init; }

    /// <summary>
    /// Gets the embedding model used for the stored vectors.
    /// </summary>
    public string EmbeddingModel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the vector dimensions of the stored embeddings.
    /// </summary>
    public int Dimensions { get; init; }

    /// <summary>
    /// Gets the timestamp when vectors were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Gets the storage size of the vector embeddings.
    /// </summary>
    public long StorageSizeBytes { get; init; }
}

/// <summary>
/// Result of vector integrity validation.
/// </summary>
public record VectorValidationResult
{
    /// <summary>
    /// Gets a value indicating whether all vectors passed validation.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the number of vectors that passed validation.
    /// </summary>
    public int ValidVectorCount { get; init; }

    /// <summary>
    /// Gets the number of vectors that failed validation.
    /// </summary>
    public int InvalidVectorCount { get; init; }

    /// <summary>
    /// Gets the validation issues found.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = [];

    /// <summary>
    /// Gets suggestions for resolving validation issues.
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; } = [];
}
