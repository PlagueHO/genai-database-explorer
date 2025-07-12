namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Builder interface for creating SemanticModelRepositoryOptions with fluent interface.
/// Implements immutable builder pattern for thread-safe configuration construction.
/// </summary>
public interface ISemanticModelRepositoryOptionsBuilder
{
    /// <summary>
    /// Configures lazy loading for entity collections.
    /// </summary>
    /// <param name="enabled">True to enable lazy loading, false to disable.</param>
    /// <returns>A new builder instance with lazy loading configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithLazyLoading(bool enabled = true);

    /// <summary>
    /// Configures change tracking for selective persistence.
    /// </summary>
    /// <param name="enabled">True to enable change tracking, false to disable.</param>
    /// <returns>A new builder instance with change tracking configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithChangeTracking(bool enabled = true);

    /// <summary>
    /// Configures caching for loaded models.
    /// </summary>
    /// <param name="enabled">True to enable caching, false to disable.</param>
    /// <returns>A new builder instance with caching configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled = true);

    /// <summary>
    /// Configures caching for loaded models with specific expiration time.
    /// </summary>
    /// <param name="enabled">True to enable caching, false to disable.</param>
    /// <param name="expiration">The cache expiration time.</param>
    /// <returns>A new builder instance with caching configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled, TimeSpan expiration);

    /// <summary>
    /// Configures the persistence strategy to use.
    /// </summary>
    /// <param name="strategyName">The name of the persistence strategy (e.g., "LocalDisk", "AzureBlob", "Cosmos").</param>
    /// <returns>A new builder instance with strategy configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithStrategyName(string strategyName);

    /// <summary>
    /// Configures the maximum number of concurrent operations.
    /// </summary>
    /// <param name="maxOperations">The maximum number of concurrent operations allowed.</param>
    /// <returns>A new builder instance with concurrency configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithMaxConcurrentOperations(int maxOperations);

    /// <summary>
    /// Configures performance monitoring options using a nested builder.
    /// </summary>
    /// <param name="configure">Action to configure the performance monitoring options.</param>
    /// <returns>A new builder instance with performance monitoring configuration applied.</returns>
    ISemanticModelRepositoryOptionsBuilder WithPerformanceMonitoring(Action<IPerformanceMonitoringOptionsBuilder> configure);

    /// <summary>
    /// Creates an immutable SemanticModelRepositoryOptions instance with all configured values.
    /// </summary>
    /// <returns>An immutable options instance that cannot be modified after creation.</returns>
    SemanticModelRepositoryOptions Build();
}
