namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Thread-safe immutable builder implementation for PerformanceMonitoringOptions.
/// Each method creates a new instance instead of mutating state, ensuring thread safety.
/// </summary>
public class PerformanceMonitoringOptionsBuilder : IPerformanceMonitoringOptionsBuilder
{
    private readonly PerformanceMonitoringOptions _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitoringOptionsBuilder"/> class.
    /// Private constructor - only used internally for immutable chaining.
    /// </summary>
    /// <param name="options">The current options state.</param>
    private PerformanceMonitoringOptionsBuilder(PerformanceMonitoringOptions options)
    {
        _current = options;
    }

    /// <summary>
    /// Creates a new builder instance with default performance monitoring options.
    /// This is the entry point for the fluent interface.
    /// </summary>
    /// <returns>A new builder instance ready for configuration.</returns>
    public static IPerformanceMonitoringOptionsBuilder Create()
    {
        return new PerformanceMonitoringOptionsBuilder(new PerformanceMonitoringOptions());
    }

    /// <summary>
    /// Configures local performance monitoring.
    /// Creates a new builder instance with the local monitoring setting applied.
    /// </summary>
    /// <param name="enabled">True to enable local monitoring, false to disable.</param>
    /// <returns>A new builder instance with local monitoring configuration applied.</returns>
    public IPerformanceMonitoringOptionsBuilder EnableLocalMonitoring(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new PerformanceMonitoringOptionsBuilder(_current with { EnableLocalMonitoring = enabled });
    }

    /// <summary>
    /// Configures the metrics retention period.
    /// Creates a new builder instance with the retention setting applied.
    /// </summary>
    /// <param name="retention">The time period for retaining performance metrics.</param>
    /// <returns>A new builder instance with retention configuration applied.</returns>
    /// <exception cref="ArgumentException">Thrown when retention is negative.</exception>
    public IPerformanceMonitoringOptionsBuilder WithMetricsRetention(TimeSpan retention)
    {
        if (retention < TimeSpan.Zero)
        {
            throw new ArgumentException("Metrics retention period cannot be negative", nameof(retention));
        }

        // Create new instance instead of mutating current state (immutable pattern)
        return new PerformanceMonitoringOptionsBuilder(_current with { MetricsRetentionPeriod = retention });
    }

    /// <summary>
    /// Creates an immutable PerformanceMonitoringOptions instance with all configured values.
    /// The returned options object cannot be modified after creation.
    /// </summary>
    /// <returns>An immutable options instance that cannot be modified after creation.</returns>
    public PerformanceMonitoringOptions Build()
    {
        // Return the current immutable record - no need to create a copy since records are immutable
        return _current;
    }
}
