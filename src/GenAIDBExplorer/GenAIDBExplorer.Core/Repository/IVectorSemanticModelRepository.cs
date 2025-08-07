using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.VectorEmbedding.Models;

namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Extension interface for semantic model repository with vector embedding capabilities.
/// Provides natural language search and AI-powered similarity matching for database entities.
/// </summary>
/// <remarks>
/// This interface extends ISemanticModelRepository to provide vector embedding functionality
/// while maintaining backward compatibility. It enables:
/// - Generation and storage of vector embeddings for database entities
/// - Natural language search with similarity ranking
/// - Support for multiple embedding models and dimensions
/// - Integration with existing persistence strategies
/// 
/// The implementation uses Microsoft Semantic Kernel Vector Store abstractions for
/// standardization and interoperability with various vector database providers.
/// </remarks>
public interface IVectorSemanticModelRepository : ISemanticModelRepository
{
    /// <summary>
    /// Generates and stores vector embeddings for entities in the semantic model.
    /// </summary>
    /// <param name="semanticModel">The semantic model containing entities to process.</param>
    /// <param name="modelPath">The path where vector embeddings will be stored.</param>
    /// <param name="options">Configuration options for vector generation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing processing statistics and any errors encountered.</returns>
    Task<VectorEmbeddingResult> GenerateAndStoreVectorEmbeddingsAsync(
        SemanticModel semanticModel,
        DirectoryInfo modelPath,
        VectorRepositoryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for entities similar to the provided natural language query.
    /// </summary>
    /// <typeparam name="TEntity">The type of semantic model entity to search.</typeparam>
    /// <param name="query">Natural language query to search for.</param>
    /// <param name="modelPath">The path where vector embeddings are stored.</param>
    /// <param name="options">Configuration options for vector search.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of search results ranked by similarity.</returns>
    Task<IEnumerable<VectorSearchResult<TEntity>>> SearchSimilarEntitiesAsync<TEntity>(
        string query,
        DirectoryInfo modelPath,
        VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEntity : SemanticModelEntity;

    /// <summary>
    /// Searches for tables similar to the provided natural language query.
    /// </summary>
    /// <param name="query">Natural language query to search for.</param>
    /// <param name="modelPath">The path where vector embeddings are stored.</param>
    /// <param name="options">Configuration options for vector search.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of table search results ranked by similarity.</returns>
    Task<IEnumerable<VectorSearchResult<SemanticModelTable>>> SearchSimilarTablesAsync(
        string query,
        DirectoryInfo modelPath,
        VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for views similar to the provided natural language query.
    /// </summary>
    /// <param name="query">Natural language query to search for.</param>
    /// <param name="modelPath">The path where vector embeddings are stored.</param>
    /// <param name="options">Configuration options for vector search.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of view search results ranked by similarity.</returns>
    Task<IEnumerable<VectorSearchResult<SemanticModelView>>> SearchSimilarViewsAsync(
        string query,
        DirectoryInfo modelPath,
        VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for stored procedures similar to the provided natural language query.
    /// </summary>
    /// <param name="query">Natural language query to search for.</param>
    /// <param name="modelPath">The path where vector embeddings are stored.</param>
    /// <param name="options">Configuration options for vector search.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of stored procedure search results ranked by similarity.</returns>
    Task<IEnumerable<VectorSearchResult<SemanticModelStoredProcedure>>> SearchSimilarStoredProceduresAsync(
        string query,
        DirectoryInfo modelPath,
        VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if vector embeddings exist for the specified model path.
    /// </summary>
    /// <param name="modelPath">The path to check for vector embeddings.</param>
    /// <param name="strategyName">Optional strategy name to check specific storage.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if vector embeddings exist; otherwise, false.</returns>
    Task<bool> HasVectorEmbeddingsAsync(
        DirectoryInfo modelPath,
        string? strategyName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes vector embeddings for the specified model path.
    /// </summary>
    /// <param name="modelPath">The path where vector embeddings are stored.</param>
    /// <param name="strategyName">Optional strategy name to target specific storage.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the deletion operation.</returns>
    Task DeleteVectorEmbeddingsAsync(
        DirectoryInfo modelPath,
        string? strategyName = null,
        CancellationToken cancellationToken = default);
}
