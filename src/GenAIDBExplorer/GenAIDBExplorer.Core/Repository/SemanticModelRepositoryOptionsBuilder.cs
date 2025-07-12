namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Thread-safe immutable builder implementation for SemanticModelRepositoryOptions.
/// Each method creates a new instance instead of mutating state, ensuring thread safety.
/// </summary>
public class SemanticModelRepositoryOptionsBuilder : ISemanticModelRepositoryOptionsBuilder
{
    private readonly SemanticModelRepositoryOptions _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticModelRepositoryOptionsBuilder"/> class.
    /// Private constructor - only used internally for immutable chaining.
    /// </summary>
    /// <param name="options">The current options state.</param>
    private SemanticModelRepositoryOptionsBuilder(SemanticModelRepositoryOptions options)
    {
        _current = options;
    }

    /// <summary>
    /// Creates a new builder instance with default options.
    /// This is the entry point for the fluent interface.
    /// </summary>
    /// <returns>A new builder instance ready for configuration.</returns>
    public static ISemanticModelRepositoryOptionsBuilder Create()
    {
        return new SemanticModelRepositoryOptionsBuilder(new SemanticModelRepositoryOptions());
    }

    /// <summary>
    /// Configures lazy loading for entity collections.
    /// Creates a new builder instance with the lazy loading setting applied.
    /// </summary>
    /// <param name="enabled">True to enable lazy loading, false to disable.</param>
    /// <returns>A new builder instance with lazy loading configuration applied.</returns>
    public ISemanticModelRepositoryOptionsBuilder WithLazyLoading(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableLazyLoading = enabled });
    }

    /// <summary>
    /// Configures change tracking for selective persistence.
    /// Creates a new builder instance with the change tracking setting applied.
    /// </summary>
    /// <param name="enabled">True to enable change tracking, false to disable.</param>
    /// <returns>A new builder instance with change tracking configuration applied.</returns>
    public ISemanticModelRepositoryOptionsBuilder WithChangeTracking(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableChangeTracking = enabled });
    }

    /// <summary>
    /// Configures caching for loaded models.
    /// Creates a new builder instance with the caching setting applied.
    /// </summary>
    /// <param name="enabled">True to enable caching, false to disable.</param>
    /// <returns>A new builder instance with caching configuration applied.</returns>
    public ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableCaching = enabled });
    }

    /// <summary>
    /// Configures caching for loaded models with specific expiration time.
    /// Creates a new builder instance with both caching and expiration settings applied.
    /// </summary>
    /// <param name="enabled">True to enable caching, false to disable.</param>
    /// <param name="expiration">The cache expiration time.</param>
    /// <returns>A new builder instance with caching configuration applied.</returns>
    /// <exception cref="ArgumentException">Thrown when expiration is negative.</exception>
    public ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled, TimeSpan expiration)
    {
        if (expiration < TimeSpan.Zero)
        {
            throw new ArgumentException("Cache expiration cannot be negative", nameof(expiration));
        }

        // Create new instance with multiple properties (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with
        {
            EnableCaching = enabled,
            CacheExpiration = expiration
        });
    }

    /// <summary>
    /// Configures the persistence strategy to use.
    /// Creates a new builder instance with the strategy name applied.
    /// </summary>
    /// <param name="strategyName">The name of the persistence strategy.</param>
    /// <returns>A new builder instance with strategy configuration applied.</returns>
    /// <exception cref="ArgumentException">Thrown when strategy name is null or whitespace.</exception>
    public ISemanticModelRepositoryOptionsBuilder WithStrategyName(string strategyName)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("Strategy name cannot be null or whitespace", nameof(strategyName));
        }

        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { StrategyName = strategyName });
    }

    /// <summary>
    /// Configures the maximum number of concurrent operations.
    /// Creates a new builder instance with the concurrency limit applied.
    /// </summary>
    /// <param name="maxOperations">The maximum number of concurrent operations allowed.</param>
    /// <returns>A new builder instance with concurrency configuration applied.</returns>
    /// <exception cref="ArgumentException">Thrown when maxOperations is less than 1.</exception>
    public ISemanticModelRepositoryOptionsBuilder WithMaxConcurrentOperations(int maxOperations)
    {
        if (maxOperations < 1)
        {
            throw new ArgumentException("Max concurrent operations must be at least 1", nameof(maxOperations));
        }

        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { MaxConcurrentOperations = maxOperations });
    }

    /// <summary>
    /// Configures performance monitoring options using a nested builder.
    /// Creates a new builder instance with the performance monitoring configuration applied.
    /// </summary>
    /// <param name="configure">Action to configure the performance monitoring options.</param>
    /// <returns>A new builder instance with performance monitoring configuration applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configure action is null.</exception>
    public ISemanticModelRepositoryOptionsBuilder WithPerformanceMonitoring(Action<IPerformanceMonitoringOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = PerformanceMonitoringOptionsBuilder.Create();
        configure(builder);
        var performanceOptions = builder.Build();

        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { PerformanceMonitoring = performanceOptions });
    }

    /// <summary>
    /// Creates an immutable SemanticModelRepositoryOptions instance with all configured values.
    /// The returned options object cannot be modified after creation.
    /// </summary>
    /// <returns>An immutable options instance that cannot be modified after creation.</returns>
    public SemanticModelRepositoryOptions Build()
    {
        // Validate configuration combinations
        if (_current.EnableCaching && _current.CacheExpiration.HasValue && _current.CacheExpiration.Value < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Cache expiration cannot be negative when caching is enabled");
        }

        // Return the current immutable record - no need to create a copy since records are immutable
        return _current;
    }
}
