namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Builder interface for creating PerformanceMonitoringOptions with fluent interface.
/// Implements immutable builder pattern for thread-safe configuration construction.
/// </summary>
public interface IPerformanceMonitoringOptionsBuilder
{
    /// <summary>
    /// Configures local performance monitoring.
    /// </summary>
    /// <param name="enabled">True to enable local monitoring, false to disable.</param>
    /// <returns>A new builder instance with local monitoring configuration applied.</returns>
    IPerformanceMonitoringOptionsBuilder EnableLocalMonitoring(bool enabled = true);

    /// <summary>
    /// Configures the metrics retention period.
    /// </summary>
    /// <param name="retention">The time period for retaining performance metrics.</param>
    /// <returns>A new builder instance with retention configuration applied.</returns>
    IPerformanceMonitoringOptionsBuilder WithMetricsRetention(TimeSpan retention);

    /// <summary>
    /// Creates an immutable PerformanceMonitoringOptions instance with all configured values.
    /// </summary>
    /// <returns>An immutable options instance that cannot be modified after creation.</returns>
    PerformanceMonitoringOptions Build();
}
