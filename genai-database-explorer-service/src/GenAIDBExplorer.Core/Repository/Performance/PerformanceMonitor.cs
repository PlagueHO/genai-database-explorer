#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Performance;

/// <summary>
/// Thread-safe implementation of <see cref="IPerformanceMonitor"/> optimized for multi-threaded web API usage.
/// Uses lock-free concurrent collections for high performance with eventual consistency.
/// </summary>
public sealed class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<OperationRecord>> _operationRecords;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationRecords = new ConcurrentDictionary<string, ConcurrentBag<OperationRecord>>();
    }

    /// <inheritdoc />
    public IPerformanceTrackingContext StartOperation(string operationName, IDictionary<string, object>? metadata = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        _logger.LogDebug("Starting performance tracking for operation: {OperationName}", operationName);

        return new PerformanceTrackingContext(operationName, this, metadata ?? new Dictionary<string, object>());
    }

    /// <inheritdoc />
    public async Task RecordOperationAsync(string operationName, TimeSpan duration, bool success, IDictionary<string, object>? metadata = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var record = new OperationRecord(operationName, duration, success, DateTime.UtcNow, metadata ?? new Dictionary<string, object>());

        // Lock-free operation using ConcurrentBag - thread-safe and high-performance
        var operationBag = _operationRecords.GetOrAdd(operationName, _ => new ConcurrentBag<OperationRecord>());
        operationBag.Add(record);

        _logger.LogDebug(
            "Recorded operation: {OperationName}, Duration: {Duration}ms, Success: {Success}",
            operationName,
            duration.TotalMilliseconds,
            success);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PerformanceMetrics> GetMetricsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var operationStatistics = new Dictionary<string, OperationStatistics>();
        long totalOperations = 0;
        long successfulOperations = 0;
        var totalDuration = TimeSpan.Zero;

        // Snapshot the current state - eventual consistency is acceptable
        foreach (var kvp in _operationRecords)
        {
            var operationName = kvp.Key;
            var operationBag = kvp.Value;

            // Convert ConcurrentBag to array for safe enumeration
            var records = operationBag.ToArray();

            if (records.Length == 0)
                continue;

            var successCount = records.Count(r => r.Success);
            var durations = records.Select(r => r.Duration).ToArray();
            var avgDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
            var minDuration = durations.Min();
            var maxDuration = durations.Max();
            var operationTotalDuration = TimeSpan.FromTicks(durations.Sum(d => d.Ticks));

            operationStatistics[operationName] = new OperationStatistics(
                operationName,
                records.Length,
                successCount,
                avgDuration,
                minDuration,
                maxDuration,
                operationTotalDuration);

            totalOperations += records.Length;
            successfulOperations += successCount;
            totalDuration = totalDuration.Add(operationTotalDuration);
        }

        var averageDuration = totalOperations > 0
            ? TimeSpan.FromTicks(totalDuration.Ticks / totalOperations)
            : TimeSpan.Zero;

        var metrics = new PerformanceMetrics(
            totalOperations,
            successfulOperations,
            averageDuration,
            totalDuration,
            operationStatistics);

        _logger.LogInformation(
            "Retrieved performance metrics: {TotalOperations} operations, {SuccessRate:F2}% success rate, {AverageDuration}ms average duration",
            metrics.TotalOperations,
            metrics.SuccessRate,
            metrics.AverageDuration.TotalMilliseconds);

        return await Task.FromResult(metrics).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PerformanceRecommendation>> GetRecommendationsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metrics = await GetMetricsAsync().ConfigureAwait(false);
        var recommendations = new List<PerformanceRecommendation>();

        // Analyze overall success rate
        if (metrics.SuccessRate < 95 && metrics.TotalOperations > 10)
        {
            recommendations.Add(new PerformanceRecommendation(
                "Reliability",
                RecommendationSeverity.High,
                $"Low success rate detected: {metrics.SuccessRate:F2}%. Consider implementing retry mechanisms or error handling improvements.",
                suggestedActions: [
                    "Implement exponential backoff retry policies",
                    "Add circuit breaker patterns for external dependencies",
                    "Review error logs for common failure patterns"
                ]));
        }

        // Analyze average duration
        if (metrics.AverageDuration > TimeSpan.FromSeconds(5))
        {
            recommendations.Add(new PerformanceRecommendation(
                "Performance",
                RecommendationSeverity.Medium,
                $"High average operation duration detected: {metrics.AverageDuration.TotalSeconds:F2} seconds. Consider performance optimizations.",
                suggestedActions: [
                    "Enable caching for frequently accessed data",
                    "Consider parallel execution for bulk operations",
                    "Review database query performance"
                ]));
        }

        // Analyze individual operation performance
        foreach (var operationStat in metrics.OperationStatistics.Values)
        {
            if (operationStat.SuccessRate < 90 && operationStat.Count > 5)
            {
                recommendations.Add(new PerformanceRecommendation(
                    "Operation Reliability",
                    RecommendationSeverity.Medium,
                    $"Operation '{operationStat.OperationName}' has low success rate: {operationStat.SuccessRate:F2}%",
                    operationStat.OperationName,
                    [
                        $"Review implementation of {operationStat.OperationName} operation",
                        "Add specific error handling for this operation type",
                        "Consider operation-specific retry logic"
                    ]));
            }

            if (operationStat.AverageDuration > TimeSpan.FromSeconds(10))
            {
                recommendations.Add(new PerformanceRecommendation(
                    "Operation Performance",
                    RecommendationSeverity.Medium,
                    $"Operation '{operationStat.OperationName}' has high average duration: {operationStat.AverageDuration.TotalSeconds:F2} seconds",
                    operationStat.OperationName,
                    [
                        $"Optimize {operationStat.OperationName} operation implementation",
                        "Consider breaking large operations into smaller batches",
                        "Implement lazy loading if applicable"
                    ]));
            }

            // Detect operations with high variance - FIXED: More sensitive threshold for better detection
            // Changed from 10x to 3x average for more realistic variance detection
            if (operationStat.MaxDuration > TimeSpan.FromTicks(operationStat.AverageDuration.Ticks * 3) && operationStat.Count > 3)
            {
                recommendations.Add(new PerformanceRecommendation(
                    "Performance Consistency",
                    RecommendationSeverity.Low,
                    $"Operation '{operationStat.OperationName}' shows high performance variance (max: {operationStat.MaxDuration.TotalSeconds:F2}s, avg: {operationStat.AverageDuration.TotalSeconds:F2}s)",
                    operationStat.OperationName,
                    [
                        "Investigate occasional performance spikes",
                        "Consider implementing operation timeouts",
                        "Monitor system resources during peak usage"
                    ]));
            }
        }

        // Memory usage recommendations
        if (metrics.TotalOperations > 1000)
        {
            recommendations.Add(new PerformanceRecommendation(
                "Resource Management",
                RecommendationSeverity.Info,
                "High operation volume detected. Consider periodic cleanup of performance data.",
                suggestedActions: [
                    "Implement automatic cleanup of old performance data",
                    "Consider using sliding window for metrics collection",
                    "Monitor memory usage of performance tracking"
                ]));
        }

        _logger.LogInformation("Generated {RecommendationCount} performance recommendations", recommendations.Count);

        return recommendations;
    }

    /// <inheritdoc />
    public async Task ResetMetricsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Clear all operation records - thread-safe operation
        _operationRecords.Clear();

        _logger.LogInformation("Performance metrics have been reset");

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationStatistics?> GetOperationStatisticsAsync(string operationName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (!_operationRecords.TryGetValue(operationName, out var operationBag))
        {
            return await Task.FromResult<OperationStatistics?>(null).ConfigureAwait(false);
        }

        // Convert ConcurrentBag to array for safe enumeration - eventual consistency
        var records = operationBag.ToArray();

        if (records.Length == 0)
        {
            return await Task.FromResult<OperationStatistics?>(null).ConfigureAwait(false);
        }

        var successCount = records.Count(r => r.Success);
        var durations = records.Select(r => r.Duration).ToArray();
        var avgDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
        var minDuration = durations.Min();
        var maxDuration = durations.Max();
        var totalDuration = TimeSpan.FromTicks(durations.Sum(d => d.Ticks));

        var statistics = new OperationStatistics(
            operationName,
            records.Length,
            successCount,
            avgDuration,
            minDuration,
            maxDuration,
            totalDuration);

        return await Task.FromResult(statistics).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        // Clear all operation records - thread-safe operation
        _operationRecords.Clear();
        _disposed = true;

        _logger.LogDebug("PerformanceMonitor disposed");
    }

    /// <summary>
    /// Represents a record of an operation for performance tracking.
    /// </summary>
    private sealed record OperationRecord(
        string OperationName,
        TimeSpan Duration,
        bool Success,
        DateTime Timestamp,
        IDictionary<string, object> Metadata);
}
