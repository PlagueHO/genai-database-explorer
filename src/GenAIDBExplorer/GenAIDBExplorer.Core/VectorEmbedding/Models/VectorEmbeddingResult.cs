using System;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.VectorEmbedding.Models;

/// <summary>
/// Result of vector embedding generation operation containing processing statistics and errors.
/// </summary>
public record VectorEmbeddingResult
{
    /// <summary>
    /// Gets the number of tables processed during the operation.
    /// </summary>
    public int TablesProcessed { get; init; }

    /// <summary>
    /// Gets the number of views processed during the operation.
    /// </summary>
    public int ViewsProcessed { get; init; }

    /// <summary>
    /// Gets the number of stored procedures processed during the operation.
    /// </summary>
    public int StoredProceduresProcessed { get; init; }

    /// <summary>
    /// Gets the total number of entities processed during the operation.
    /// </summary>
    public int TotalEntitiesProcessed => TablesProcessed + ViewsProcessed + StoredProceduresProcessed;

    /// <summary>
    /// Gets the number of entities that were skipped (already had current embeddings).
    /// </summary>
    public int EntitiesSkipped { get; init; }

    /// <summary>
    /// Gets the number of entities that had errors during processing.
    /// </summary>
    public int EntitiesWithErrors { get; init; }

    /// <summary>
    /// Gets the total number of tokens consumed during embedding generation.
    /// </summary>
    /// <remarks>
    /// Used for cost tracking and API quota management.
    /// </remarks>
    public int TotalTokensConsumed { get; init; }

    /// <summary>
    /// Gets the total time taken for the embedding generation operation.
    /// </summary>
    public TimeSpan TotalTimeTaken { get; init; }

    /// <summary>
    /// Gets the collection of errors encountered during processing.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets the collection of warning messages generated during processing.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully without errors.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether any entities were processed.
    /// </summary>
    public bool HasProcessedEntities => TotalEntitiesProcessed > 0;

    /// <summary>
    /// Initializes a new instance of the VectorEmbeddingResult record.
    /// </summary>
    public VectorEmbeddingResult()
    {
    }

    /// <summary>
    /// Creates a successful result with the specified processing counts.
    /// </summary>
    /// <param name="tablesProcessed">Number of tables processed.</param>
    /// <param name="viewsProcessed">Number of views processed.</param>
    /// <param name="storedProceduresProcessed">Number of stored procedures processed.</param>
    /// <param name="entitiesSkipped">Number of entities skipped.</param>
    /// <param name="tokensConsumed">Total tokens consumed.</param>
    /// <param name="timeTaken">Total time taken.</param>
    /// <param name="warnings">Optional warning messages.</param>
    /// <returns>A new VectorEmbeddingResult instance.</returns>
    public static VectorEmbeddingResult Success(
        int tablesProcessed = 0,
        int viewsProcessed = 0,
        int storedProceduresProcessed = 0,
        int entitiesSkipped = 0,
        int tokensConsumed = 0,
        TimeSpan timeTaken = default,
        IReadOnlyList<string>? warnings = null)
    {
        return new VectorEmbeddingResult
        {
            TablesProcessed = tablesProcessed,
            ViewsProcessed = viewsProcessed,
            StoredProceduresProcessed = storedProceduresProcessed,
            EntitiesSkipped = entitiesSkipped,
            TotalTokensConsumed = tokensConsumed,
            TotalTimeTaken = timeTaken,
            Warnings = warnings ?? []
        };
    }

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The errors that occurred during processing.</param>
    /// <param name="partialResults">Optional partial processing counts if some entities succeeded.</param>
    /// <returns>A new VectorEmbeddingResult instance.</returns>
    public static VectorEmbeddingResult Failure(
        IReadOnlyList<string> errors,
        VectorEmbeddingResult? partialResults = null)
    {
        return new VectorEmbeddingResult
        {
            TablesProcessed = partialResults?.TablesProcessed ?? 0,
            ViewsProcessed = partialResults?.ViewsProcessed ?? 0,
            StoredProceduresProcessed = partialResults?.StoredProceduresProcessed ?? 0,
            EntitiesSkipped = partialResults?.EntitiesSkipped ?? 0,
            EntitiesWithErrors = errors.Count,
            TotalTokensConsumed = partialResults?.TotalTokensConsumed ?? 0,
            TotalTimeTaken = partialResults?.TotalTimeTaken ?? TimeSpan.Zero,
            Errors = errors,
            Warnings = partialResults?.Warnings ?? []
        };
    }
}

/// <summary>
/// Search result containing an entity and its similarity score.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public record VectorSearchResult<TEntity> where TEntity : SemanticModelEntity
{
    /// <summary>
    /// Gets the semantic model entity that matched the search query.
    /// </summary>
    public TEntity Entity { get; init; } = default!;

    /// <summary>
    /// Gets the similarity score between the query and the entity.
    /// </summary>
    /// <remarks>
    /// Cosine similarity score between 0.0 and 1.0, where:
    /// - 1.0 indicates perfect similarity
    /// - 0.0 indicates no similarity
    /// - Values above 0.8 typically indicate strong semantic similarity
    /// - Values above 0.5 indicate moderate semantic similarity
    /// </remarks>
    public double SimilarityScore { get; init; }

    /// <summary>
    /// Gets the vector embedding for the entity, if requested in search options.
    /// </summary>
    /// <remarks>
    /// Only populated when IncludeEmbeddings is true in VectorSearchOptions.
    /// Useful for further analysis or similarity calculations.
    /// </remarks>
    public ReadOnlyMemory<float>? Embedding { get; init; }

    /// <summary>
    /// Gets the searchable text content that was used for similarity matching.
    /// </summary>
    /// <remarks>
    /// Provides insight into what content contributed to the similarity score.
    /// Useful for understanding search results and debugging relevance.
    /// </remarks>
    public string SearchableText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the embedding model used to generate the entity's vector.
    /// </summary>
    public string EmbeddingModel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the vector was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the vector was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Initializes a new instance of the VectorSearchResult record.
    /// </summary>
    public VectorSearchResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the VectorSearchResult record.
    /// </summary>
    /// <param name="entity">The semantic model entity.</param>
    /// <param name="similarityScore">The similarity score.</param>
    /// <param name="searchableText">The searchable text content.</param>
    /// <param name="embeddingModel">The embedding model used.</param>
    /// <param name="createdAt">When the vector was created.</param>
    /// <param name="lastUpdated">When the vector was last updated.</param>
    /// <param name="embedding">Optional embedding vector.</param>
    public VectorSearchResult(
        TEntity entity,
        double similarityScore,
        string searchableText,
        string embeddingModel,
        DateTimeOffset createdAt,
        DateTimeOffset lastUpdated,
        ReadOnlyMemory<float>? embedding = null)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        SimilarityScore = similarityScore;
        SearchableText = searchableText ?? throw new ArgumentNullException(nameof(searchableText));
        EmbeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));
        CreatedAt = createdAt;
        LastUpdated = lastUpdated;
        Embedding = embedding;
    }

    /// <summary>
    /// Creates a search result from a vector record and similarity score.
    /// </summary>
    /// <param name="vectorRecord">The vector record containing the entity and metadata.</param>
    /// <param name="similarityScore">The calculated similarity score.</param>
    /// <param name="includeEmbedding">Whether to include the embedding vector in the result.</param>
    /// <returns>A new VectorSearchResult instance.</returns>
    public static VectorSearchResult<TEntity> FromVectorRecord(
        VectorRecord<TEntity> vectorRecord,
        double similarityScore,
        bool includeEmbedding = false)
    {
        ArgumentNullException.ThrowIfNull(vectorRecord);

        return new VectorSearchResult<TEntity>(
            entity: vectorRecord.Entity,
            similarityScore: similarityScore,
            searchableText: vectorRecord.SearchableText,
            embeddingModel: vectorRecord.EmbeddingModel,
            createdAt: vectorRecord.CreatedAt,
            lastUpdated: vectorRecord.LastUpdated,
            embedding: includeEmbedding ? vectorRecord.Embedding : null);
    }
}
