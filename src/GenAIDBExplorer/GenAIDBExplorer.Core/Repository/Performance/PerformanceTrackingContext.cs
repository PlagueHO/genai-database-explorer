using System.Diagnostics;

namespace GenAIDBExplorer.Core.Repository.Performance;

/// <summary>
/// A context for tracking the performance of an operation that automatically records the operation when disposed.
/// </summary>
internal sealed class PerformanceTrackingContext : IPerformanceTrackingContext
{
    private readonly string _operationName;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly Dictionary<string, object> _metadata;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceTrackingContext"/> class.
    /// </summary>
    /// <param name="operationName">The name of the operation being tracked.</param>
    /// <param name="performanceMonitor">The performance monitor to record to.</param>
    /// <param name="metadata">Optional metadata for the operation.</param>
    public PerformanceTrackingContext(string operationName, PerformanceMonitor performanceMonitor, IDictionary<string, object> metadata)
    {
        OperationName = operationName;
        _operationName = operationName;
        _performanceMonitor = performanceMonitor;
        _metadata = new Dictionary<string, object>(metadata);
        Metadata = _metadata;
        _stopwatch = Stopwatch.StartNew();
        Stopwatch = _stopwatch;
        Success = true;
    }

    /// <inheritdoc />
    public string OperationName { get; }

    /// <inheritdoc />
    public Stopwatch Stopwatch { get; }

    /// <inheritdoc />
    public bool Success { get; set; } = true;

    /// <inheritdoc />
    public IDictionary<string, object> Metadata { get; }

    /// <inheritdoc />
    public void AddMetadata(string key, object value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _metadata[key] = value;
    }

    /// <inheritdoc />
    public void MarkAsFailed(string? errorMessage = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Success = false;

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            _metadata["ErrorMessage"] = errorMessage;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _stopwatch.Stop();

        try
        {
            // Use synchronous recording to avoid race conditions in tests
            // The RecordOperationAsync method returns a completed task for this use case
            _performanceMonitor.RecordOperationAsync(_operationName, _stopwatch.Elapsed, Success, _metadata)
                .GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore exceptions during disposal to prevent issues in using statements
        }

        _disposed = true;
    }
}
