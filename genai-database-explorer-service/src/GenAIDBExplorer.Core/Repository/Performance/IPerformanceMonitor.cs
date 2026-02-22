#nullable enable

using System.Diagnostics;

namespace GenAIDBExplorer.Core.Repository.Performance;

/// <summary>
/// Interface for monitoring and tracking performance metrics of repository operations.
/// </summary>
public interface IPerformanceMonitor : IDisposable
{
    /// <summary>
    /// Starts monitoring a repository operation.
    /// </summary>
    /// <param name="operationName">The name of the operation being monitored.</param>
    /// <param name="metadata">Optional metadata about the operation.</param>
    /// <returns>A performance tracking context that should be disposed when the operation completes.</returns>
    IPerformanceTrackingContext StartOperation(string operationName, IDictionary<string, object>? metadata = null);

    /// <summary>
    /// Records a completed operation with its duration and outcome.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="metadata">Optional metadata about the operation.</param>
    Task RecordOperationAsync(string operationName, TimeSpan duration, bool success, IDictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets current performance metrics for all monitored operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the performance metrics.</returns>
    Task<PerformanceMetrics> GetMetricsAsync();

    /// <summary>
    /// Gets performance recommendations based on collected metrics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the performance recommendations.</returns>
    Task<IReadOnlyList<PerformanceRecommendation>> GetRecommendationsAsync();

    /// <summary>
    /// Resets all collected performance metrics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ResetMetricsAsync();

    /// <summary>
    /// Gets performance statistics for a specific operation type.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation statistics.</returns>
    Task<OperationStatistics?> GetOperationStatisticsAsync(string operationName);
}

/// <summary>
/// Interface for tracking the performance of an individual operation.
/// </summary>
public interface IPerformanceTrackingContext : IDisposable
{
    /// <summary>
    /// Gets the name of the operation being tracked.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the stopwatch used to measure the operation duration.
    /// </summary>
    Stopwatch Stopwatch { get; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    bool Success { get; set; }

    /// <summary>
    /// Gets the metadata associated with this operation.
    /// </summary>
    IDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Adds metadata to the tracking context.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    void AddMetadata(string key, object value);

    /// <summary>
    /// Marks the operation as failed with an optional error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    void MarkAsFailed(string? errorMessage = null);
}

/// <summary>
/// Represents performance metrics for repository operations.
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMetrics"/> class.
    /// </summary>
    /// <param name="totalOperations">The total number of operations performed.</param>
    /// <param name="successfulOperations">The number of successful operations.</param>
    /// <param name="averageDuration">The average duration of operations.</param>
    /// <param name="totalDuration">The total duration of all operations.</param>
    /// <param name="operationStatistics">Statistics for individual operation types.</param>
    public PerformanceMetrics(
        long totalOperations,
        long successfulOperations,
        TimeSpan averageDuration,
        TimeSpan totalDuration,
        IReadOnlyDictionary<string, OperationStatistics> operationStatistics)
    {
        TotalOperations = totalOperations;
        SuccessfulOperations = successfulOperations;
        AverageDuration = averageDuration;
        TotalDuration = totalDuration;
        OperationStatistics = operationStatistics;
    }

    /// <summary>
    /// Gets the total number of operations performed.
    /// </summary>
    public long TotalOperations { get; }

    /// <summary>
    /// Gets the number of successful operations.
    /// </summary>
    public long SuccessfulOperations { get; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;

    /// <summary>
    /// Gets the average duration of operations.
    /// </summary>
    public TimeSpan AverageDuration { get; }

    /// <summary>
    /// Gets the total duration of all operations.
    /// </summary>
    public TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets statistics for individual operation types.
    /// </summary>
    public IReadOnlyDictionary<string, OperationStatistics> OperationStatistics { get; }
}

/// <summary>
/// Represents statistics for a specific operation type.
/// </summary>
public sealed record OperationStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStatistics"/> class.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="count">The number of times the operation was performed.</param>
    /// <param name="successCount">The number of successful operations.</param>
    /// <param name="averageDuration">The average duration of the operation.</param>
    /// <param name="minDuration">The minimum duration of the operation.</param>
    /// <param name="maxDuration">The maximum duration of the operation.</param>
    /// <param name="totalDuration">The total duration of all operations of this type.</param>
    public OperationStatistics(
        string operationName,
        long count,
        long successCount,
        TimeSpan averageDuration,
        TimeSpan minDuration,
        TimeSpan maxDuration,
        TimeSpan totalDuration)
    {
        OperationName = operationName;
        Count = count;
        SuccessCount = successCount;
        AverageDuration = averageDuration;
        MinDuration = minDuration;
        MaxDuration = maxDuration;
        TotalDuration = totalDuration;
    }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the number of times the operation was performed.
    /// </summary>
    public long Count { get; }

    /// <summary>
    /// Gets the number of successful operations.
    /// </summary>
    public long SuccessCount { get; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => Count > 0 ? (double)SuccessCount / Count * 100 : 0;

    /// <summary>
    /// Gets the average duration of the operation.
    /// </summary>
    public TimeSpan AverageDuration { get; }

    /// <summary>
    /// Gets the minimum duration of the operation.
    /// </summary>
    public TimeSpan MinDuration { get; }

    /// <summary>
    /// Gets the maximum duration of the operation.
    /// </summary>
    public TimeSpan MaxDuration { get; }

    /// <summary>
    /// Gets the total duration of all operations of this type.
    /// </summary>
    public TimeSpan TotalDuration { get; }
}

/// <summary>
/// Represents a performance recommendation based on collected metrics.
/// </summary>
public sealed record PerformanceRecommendation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceRecommendation"/> class.
    /// </summary>
    /// <param name="category">The category of the recommendation.</param>
    /// <param name="severity">The severity level of the recommendation.</param>
    /// <param name="message">The recommendation message.</param>
    /// <param name="operationName">The operation this recommendation applies to, if specific.</param>
    /// <param name="suggestedActions">Suggested actions to improve performance.</param>
    public PerformanceRecommendation(
        string category,
        RecommendationSeverity severity,
        string message,
        string? operationName = null,
        IReadOnlyList<string>? suggestedActions = null)
    {
        Category = category;
        Severity = severity;
        Message = message;
        OperationName = operationName;
        SuggestedActions = suggestedActions ?? [];
    }

    /// <summary>
    /// Gets the category of the recommendation.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the severity level of the recommendation.
    /// </summary>
    public RecommendationSeverity Severity { get; }

    /// <summary>
    /// Gets the recommendation message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the operation this recommendation applies to, if specific.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    /// Gets the suggested actions to improve performance.
    /// </summary>
    public IReadOnlyList<string> SuggestedActions { get; }
}

/// <summary>
/// Represents the severity level of a performance recommendation.
/// </summary>
public enum RecommendationSeverity
{
    /// <summary>
    /// Informational recommendation.
    /// </summary>
    Info,

    /// <summary>
    /// Low severity recommendation.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity recommendation.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity recommendation.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity recommendation.
    /// </summary>
    Critical
}
