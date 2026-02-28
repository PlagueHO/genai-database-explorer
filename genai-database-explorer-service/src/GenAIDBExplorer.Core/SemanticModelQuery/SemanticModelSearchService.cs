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

    private async Task<IReadOnlyList<SemanticModelSearchResult>> SearchByEntityTypeAsync(
        string query, string entityType, int topK, CancellationToken cancellationToken)
    {
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
