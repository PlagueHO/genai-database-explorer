namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Immutable configuration record for semantic model repository operations.
/// Uses record type with init properties for thread-safe immutable configuration.
/// </summary>
public record SemanticModelRepositoryOptions
{
    /// <summary>
    /// Gets a value indicating whether lazy loading is enabled for entity collections.
    /// </summary>
    public bool EnableLazyLoading { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether change tracking is enabled for selective persistence.
    /// </summary>
    public bool EnableChangeTracking { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether caching is enabled for loaded models.
    /// </summary>
    public bool EnableCaching { get; init; } = false;

    /// <summary>
    /// Gets the name of the persistence strategy to use (e.g., "LocalDisk", "AzureBlob", "CosmosDb").
    /// </summary>
    public string? StrategyName { get; init; }

    /// <summary>
    /// Gets the cache expiration time when caching is enabled.
    /// </summary>
    public TimeSpan? CacheExpiration { get; init; }

    /// <summary>
    /// Gets the maximum number of concurrent operations allowed.
    /// </summary>
    public int? MaxConcurrentOperations { get; init; }

    /// <summary>
    /// Gets the performance monitoring configuration options.
    /// </summary>
    public PerformanceMonitoringOptions? PerformanceMonitoring { get; init; }
}
