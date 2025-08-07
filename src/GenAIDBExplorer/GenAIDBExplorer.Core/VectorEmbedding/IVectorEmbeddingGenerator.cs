using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.VectorEmbedding.Models;

namespace GenAIDBExplorer.Core.VectorEmbedding;

/// <summary>
/// Service interface for generating vector embeddings from semantic model entities.
/// </summary>
/// <remarks>
/// This service follows the same patterns as SemanticDescriptionProvider, using
/// ISemanticKernelFactory for AI operations and supporting multiple embedding models.
/// 
/// The implementation handles:
/// - Text aggregation from entity metadata
/// - Batch processing for API efficiency
/// - Token usage tracking and cost monitoring
/// - Parallel processing with configurable concurrency
/// - Error handling with partial results
/// </remarks>
public interface IVectorEmbeddingGenerator
{
    /// <summary>
    /// Generates a vector embedding for a single semantic model entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="entity">The entity to generate an embedding for.</param>
    /// <param name="embeddingModel">The embedding model to use.</param>
    /// <param name="dimensions">The expected number of dimensions in the output vector.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A memory containing the embedding vector.</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync<TEntity>(
        TEntity entity,
        string embeddingModel = "text-embedding-ada-002",
        int dimensions = 1536,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Generates vector embeddings for a collection of semantic model entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="entities">The entities to generate embeddings for.</param>
    /// <param name="options">Configuration options for the generation process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of vector records with generated embeddings.</returns>
    Task<IEnumerable<VectorRecord<TEntity>>> GenerateEmbeddingsAsync<TEntity>(
        IEnumerable<TEntity> entities,
        VectorRepositoryOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Generates vector embeddings for all entities in a semantic model.
    /// </summary>
    /// <param name="semanticModel">The semantic model containing entities to process.</param>
    /// <param name="options">Configuration options for the generation process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Processing result with statistics and any errors encountered.</returns>
    Task<VectorEmbeddingResult> GenerateEmbeddingsForModelAsync(
        SemanticModel semanticModel,
        VectorRepositoryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a query embedding for natural language search.
    /// </summary>
    /// <param name="query">The natural language query to embed.</param>
    /// <param name="embeddingModel">The embedding model to use (must match stored embeddings).</param>
    /// <param name="dimensions">The expected number of dimensions in the output vector.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A memory containing the query embedding vector.</returns>
    Task<ReadOnlyMemory<float>> GenerateQueryEmbeddingAsync(
        string query,
        string embeddingModel = "text-embedding-ada-002",
        int dimensions = 1536,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates entity metadata into searchable text content for embedding generation.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
    /// <param name="entity">The entity to process.</param>
    /// <returns>Aggregated text content suitable for embedding generation.</returns>
    /// <remarks>
    /// Combines entity information in a structured way:
    /// - Entity name and schema information
    /// - Description and semantic description
    /// - Column information (for tables and views)
    /// - Parameters and definition (for stored procedures)
    /// - Related entity information where applicable
    /// </remarks>
    string AggregateEntityText<TEntity>(TEntity entity)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Validates that the specified embedding model is supported.
    /// </summary>
    /// <param name="embeddingModel">The embedding model to validate.</param>
    /// <param name="dimensions">The expected dimensions for the model.</param>
    /// <returns>True if the model is supported; otherwise, false.</returns>
    /// <remarks>
    /// Supported models and their dimensions:
    /// - text-embedding-ada-002: 1536 dimensions
    /// - text-embedding-3-small: 1536 dimensions
    /// - text-embedding-3-large: 3072 dimensions
    /// </remarks>
    bool IsEmbeddingModelSupported(string embeddingModel, int dimensions);

    /// <summary>
    /// Gets the default dimensions for the specified embedding model.
    /// </summary>
    /// <param name="embeddingModel">The embedding model.</param>
    /// <returns>The default number of dimensions for the model.</returns>
    /// <exception cref="ArgumentException">Thrown when the model is not supported.</exception>
    int GetDefaultDimensions(string embeddingModel);
}
