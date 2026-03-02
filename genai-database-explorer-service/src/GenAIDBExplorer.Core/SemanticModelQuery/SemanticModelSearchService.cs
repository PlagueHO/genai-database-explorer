using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Provides vector-based search across semantic model entities with entity type filtering.
/// Wraps existing <see cref="IVectorSearchService"/> and <see cref="IEmbeddingGenerator"/> infrastructure.
/// </summary>
public sealed class SemanticModelSearchService(
    IEmbeddingGenerator embeddingGenerator,
    IVectorSearchService vectorSearchService,
    IVectorInfrastructureFactory vectorInfrastructureFactory,
    IProject project,
    ILogger<SemanticModelSearchService> logger
) : ISemanticModelSearchService
{
    private readonly IEmbeddingGenerator _embeddingGenerator = embeddingGenerator;
    private readonly IVectorSearchService _vectorSearchService = vectorSearchService;
    private readonly IVectorInfrastructureFactory _vectorInfrastructureFactory = vectorInfrastructureFactory;
    private readonly IProject _project = project;
    private readonly ILogger<SemanticModelSearchService> _logger = logger;

    /// <summary>
    /// Over-fetch multiplier to ensure enough results of the target entity type after filtering.
    /// </summary>
    private const int OverFetchMultiplier = 3;

    /// <summary>
    /// Maximum number of results returned by <see cref="SearchAsync"/>.
    /// </summary>
    private const int MaxTopK = 10;

    /// <summary>
    /// Minimum cosine similarity score for a result to be included in <see cref="SearchAsync"/> output.
    /// </summary>
    private const double MinimumScoreThreshold = 0.3;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticModelSearchResult>> SearchTablesAsync(
        string query, int topK, CancellationToken cancellationToken = default)
    {
        return await SearchByEntityTypeAsync(query, "Table", topK, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticModelSearchResult>> SearchViewsAsync(
        string query, int topK, CancellationToken cancellationToken = default)
    {
        return await SearchByEntityTypeAsync(query, "View", topK, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticModelSearchResult>> SearchStoredProceduresAsync(
        string query, int topK, CancellationToken cancellationToken = default)
    {
        return await SearchByEntityTypeAsync(query, "StoredProcedure", topK, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticModelSearchResult>> SearchAsync(
        string query,
        int topK,
        IReadOnlyList<string>? entityTypes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        topK = Math.Min(topK, MaxTopK);

        var infrastructure = _vectorInfrastructureFactory.Create(
            _project.Settings.VectorIndex,
            _project.Settings.SemanticModel.PersistenceStrategy);

        _logger.LogDebug(
            "Generating embedding for unified search query: {Query}, TopK: {TopK}",
            query, topK);

        var embedding = await _embeddingGenerator.GenerateAsync(query, infrastructure, cancellationToken);

        var overFetchTopK = topK * OverFetchMultiplier;
        var results = await _vectorSearchService.SearchAsync(embedding, overFetchTopK, infrastructure, cancellationToken);

        var filtered = results.AsEnumerable();

        if (entityTypes is not null && entityTypes.Count > 0)
        {
            filtered = filtered.Where(r => entityTypes.Any(et =>
                string.Equals(r.Record.EntityType, et, StringComparison.OrdinalIgnoreCase)));
        }

        filtered = filtered.Where(r => r.Score >= MinimumScoreThreshold);

        var resultList = filtered
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .Select(r => new SemanticModelSearchResult(
                EntityType: r.Record.EntityType ?? string.Empty,
                SchemaName: r.Record.Schema ?? string.Empty,
                EntityName: r.Record.Name ?? string.Empty,
                Content: r.Record.Content,
                Score: r.Score))
            .ToList();

        _logger.LogDebug(
            "Unified search completed for Query: {Query}, EntityTypes: {EntityTypes}, Results: {Count}",
            query,
            entityTypes is null ? "all" : string.Join(",", entityTypes),
            resultList.Count);

        return resultList;
    }

    private async Task<IReadOnlyList<SemanticModelSearchResult>> SearchByEntityTypeAsync(
        string query, string entityType, int topK, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        var infrastructure = _vectorInfrastructureFactory.Create(
            _project.Settings.VectorIndex,
            _project.Settings.SemanticModel.PersistenceStrategy);

        _logger.LogDebug(
            "Generating embedding for search query: {Query}, EntityType: {EntityType}",
            query, entityType);

        var embedding = await _embeddingGenerator.GenerateAsync(query, infrastructure, cancellationToken);

        var overFetchTopK = topK * OverFetchMultiplier;
        var results = await _vectorSearchService.SearchAsync(embedding, overFetchTopK, infrastructure, cancellationToken);

        var filtered = results
            .Where(r => string.Equals(r.Record.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
            .Take(topK)
            .Select(r => new SemanticModelSearchResult(
                EntityType: r.Record.EntityType ?? entityType,
                SchemaName: r.Record.Schema ?? string.Empty,
                EntityName: r.Record.Name ?? string.Empty,
                Content: r.Record.Content,
                Score: r.Score))
            .ToList();

        _logger.LogDebug(
            "Search completed for EntityType: {EntityType}, Query: {Query}, Results: {Count}",
            entityType, query, filtered.Count);

        return filtered;
    }
}
