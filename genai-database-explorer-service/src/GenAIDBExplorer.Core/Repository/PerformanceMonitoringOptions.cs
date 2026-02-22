namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Immutable configuration record for performance monitoring and telemetry.
/// Uses record type with init properties for thread-safe configuration.
/// </summary>
public record PerformanceMonitoringOptions
{
    /// <summary>
    /// Gets a value indicating whether local performance monitoring is enabled.
    /// </summary>
    public bool EnableLocalMonitoring { get; init; } = true;

    /// <summary>
    /// Gets the metrics retention period for performance data.
    /// </summary>
    public TimeSpan? MetricsRetentionPeriod { get; init; } = TimeSpan.FromHours(24);
}
