#nullable enable

using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Performance;

/// <summary>
/// Unit tests for <see cref="PerformanceMonitor"/>.
/// </summary>
[TestClass]
public sealed class PerformanceMonitorTests
{
    private Mock<ILogger<PerformanceMonitor>> _mockLogger = null!;
    private PerformanceMonitor _performanceMonitor = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<PerformanceMonitor>>();
        _performanceMonitor = new PerformanceMonitor(_mockLogger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _performanceMonitor?.Dispose();
    }

    [TestMethod]
    public void Constructor_WithValidLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        using var monitor = new PerformanceMonitor(_mockLogger.Object);

        // Assert
        monitor.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new PerformanceMonitor(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [TestMethod]
    public void StartOperation_WithValidOperationName_ShouldReturnTrackingContext()
    {
        // Arrange
        const string operationName = "TestOperation";

        // Act
        using var context = _performanceMonitor.StartOperation(operationName);

        // Assert
        context.Should().NotBeNull();
        context.OperationName.Should().Be(operationName);
        context.Success.Should().BeTrue();
        context.Stopwatch.Should().NotBeNull();
        context.Stopwatch.IsRunning.Should().BeTrue();
        context.Metadata.Should().NotBeNull();
    }

    [TestMethod]
    public void StartOperation_WithNullOperationName_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        var action = () => _performanceMonitor.StartOperation(null!);
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void StartOperation_WithEmptyOperationName_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        var action = () => _performanceMonitor.StartOperation(string.Empty);
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void StartOperation_WithMetadata_ShouldIncludeMetadataInContext()
    {
        // Arrange
        const string operationName = "TestOperation";
        var metadata = new Dictionary<string, object>
        {
            ["TestKey"] = "TestValue",
            ["NumericKey"] = 42
        };

        // Act
        using var context = _performanceMonitor.StartOperation(operationName, metadata);

        // Assert
        context.Metadata.Should().ContainKeys("TestKey", "NumericKey");
        context.Metadata["TestKey"].Should().Be("TestValue");
        context.Metadata["NumericKey"].Should().Be(42);
    }

    [TestMethod]
    public async Task RecordOperationAsync_WithValidData_ShouldRecordSuccessfully()
    {
        // Arrange
        const string operationName = "TestOperation";
        var duration = TimeSpan.FromMilliseconds(100);
        const bool success = true;

        // Act
        await _performanceMonitor.RecordOperationAsync(operationName, duration, success);

        // Assert
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync(operationName);
        statistics.Should().NotBeNull();
        statistics!.OperationName.Should().Be(operationName);
        statistics.Count.Should().Be(1);
        statistics.SuccessCount.Should().Be(1);
        statistics.SuccessRate.Should().Be(100);
    }

    [TestMethod]
    public async Task RecordOperationAsync_WithMultipleOperations_ShouldAggregateCorrectly()
    {
        // Arrange
        const string operationName = "TestOperation";
        var durations = new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(300)
        };

        // Act
        await _performanceMonitor.RecordOperationAsync(operationName, durations[0], true);
        await _performanceMonitor.RecordOperationAsync(operationName, durations[1], false);
        await _performanceMonitor.RecordOperationAsync(operationName, durations[2], true);

        // Assert
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync(operationName);
        statistics.Should().NotBeNull();
        statistics!.Count.Should().Be(3);
        statistics.SuccessCount.Should().Be(2);
        statistics.SuccessRate.Should().BeApproximately(66.67, 0.01);
        statistics.MinDuration.Should().Be(durations[0]);
        statistics.MaxDuration.Should().Be(durations[2]);
    }

    [TestMethod]
    public async Task GetMetricsAsync_WithNoOperations_ShouldReturnEmptyMetrics()
    {
        // Arrange & Act
        var metrics = await _performanceMonitor.GetMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalOperations.Should().Be(0);
        metrics.SuccessfulOperations.Should().Be(0);
        metrics.SuccessRate.Should().Be(0);
        metrics.AverageDuration.Should().Be(TimeSpan.Zero);
        metrics.TotalDuration.Should().Be(TimeSpan.Zero);
        metrics.OperationStatistics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetMetricsAsync_WithOperations_ShouldReturnCorrectMetrics()
    {
        // Arrange
        await _performanceMonitor.RecordOperationAsync("Op1", TimeSpan.FromMilliseconds(100), true);
        await _performanceMonitor.RecordOperationAsync("Op1", TimeSpan.FromMilliseconds(200), false);
        await _performanceMonitor.RecordOperationAsync("Op2", TimeSpan.FromMilliseconds(150), true);

        // Act
        var metrics = await _performanceMonitor.GetMetricsAsync();

        // Assert
        metrics.TotalOperations.Should().Be(3);
        metrics.SuccessfulOperations.Should().Be(2);
        metrics.SuccessRate.Should().BeApproximately(66.67, 0.01);
        metrics.OperationStatistics.Should().HaveCount(2);
        metrics.OperationStatistics.Should().ContainKeys("Op1", "Op2");
    }

    [TestMethod]
    public async Task GetRecommendationsAsync_WithLowSuccessRate_ShouldReturnReliabilityRecommendation()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await _performanceMonitor.RecordOperationAsync("TestOp", TimeSpan.FromMilliseconds(100), i < 5); // 33% success rate
        }

        // Act
        var recommendations = await _performanceMonitor.GetRecommendationsAsync();

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().Contain(r => r.Category == "Reliability" && r.Severity == RecommendationSeverity.High);
    }

    [TestMethod]
    public async Task GetRecommendationsAsync_WithHighDuration_ShouldReturnPerformanceRecommendation()
    {
        // Arrange
        await _performanceMonitor.RecordOperationAsync("SlowOp", TimeSpan.FromSeconds(10), true);

        // Act
        var recommendations = await _performanceMonitor.GetRecommendationsAsync();

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().Contain(r => r.Category == "Performance" && r.Severity == RecommendationSeverity.Medium);
    }

    [TestMethod]
    public async Task GetRecommendationsAsync_WithHighVariance_ShouldReturnConsistencyRecommendation()
    {
        // Arrange
        const string operationName = "VariableOp";
        await _performanceMonitor.RecordOperationAsync(operationName, TimeSpan.FromMilliseconds(100), true);
        await _performanceMonitor.RecordOperationAsync(operationName, TimeSpan.FromMilliseconds(200), true);
        await _performanceMonitor.RecordOperationAsync(operationName, TimeSpan.FromMilliseconds(300), true);
        await _performanceMonitor.RecordOperationAsync(operationName, TimeSpan.FromMilliseconds(5000), true); // High variance

        // Act
        var recommendations = await _performanceMonitor.GetRecommendationsAsync();

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().Contain(r => r.Category == "Performance Consistency" && r.OperationName == operationName);
    }

    [TestMethod]
    public async Task ResetMetricsAsync_ShouldClearAllMetrics()
    {
        // Arrange
        await _performanceMonitor.RecordOperationAsync("TestOp", TimeSpan.FromMilliseconds(100), true);
        var initialMetrics = await _performanceMonitor.GetMetricsAsync();
        initialMetrics.TotalOperations.Should().Be(1);

        // Act
        await _performanceMonitor.ResetMetricsAsync();

        // Assert
        var metricsAfterReset = await _performanceMonitor.GetMetricsAsync();
        metricsAfterReset.TotalOperations.Should().Be(0);
        metricsAfterReset.OperationStatistics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOperationStatisticsAsync_WithNonExistentOperation_ShouldReturnNull()
    {
        // Arrange & Act
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("NonExistentOperation");

        // Assert
        statistics.Should().BeNull();
    }

    [TestMethod]
    public void StartOperation_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        var action = () => _performanceMonitor.StartOperation("TestOp");
        action.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task RecordOperationAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        var action = async () => await _performanceMonitor.RecordOperationAsync("TestOp", TimeSpan.FromMilliseconds(100), true);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task GetMetricsAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        var action = async () => await _performanceMonitor.GetMetricsAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [TestMethod]
    public void TrackingContext_AddMetadata_ShouldUpdateMetadata()
    {
        // Arrange
        using var context = _performanceMonitor.StartOperation("TestOp");

        // Act
        context.AddMetadata("NewKey", "NewValue");

        // Assert
        context.Metadata.Should().ContainKey("NewKey");
        context.Metadata["NewKey"].Should().Be("NewValue");
    }

    [TestMethod]
    public void TrackingContext_MarkAsFailed_ShouldSetSuccessToFalse()
    {
        // Arrange
        using var context = _performanceMonitor.StartOperation("TestOp");
        context.Success.Should().BeTrue(); // Initial state

        // Act
        context.MarkAsFailed("Test error");

        // Assert
        context.Success.Should().BeFalse();
        context.Metadata.Should().ContainKey("ErrorMessage");
        context.Metadata["ErrorMessage"].Should().Be("Test error");
    }

    [TestMethod]
    public void TrackingContext_Dispose_ShouldStopStopwatch()
    {
        // Arrange
        var context = _performanceMonitor.StartOperation("TestOp");
        context.Stopwatch.IsRunning.Should().BeTrue();

        // Act
        context.Dispose();

        // Assert
        context.Stopwatch.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        const int operationCount = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var operationIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await _performanceMonitor.RecordOperationAsync($"ConcurrentOp{operationIndex % 5}", TimeSpan.FromMilliseconds(10), true);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var metrics = await _performanceMonitor.GetMetricsAsync();
        metrics.TotalOperations.Should().Be(operationCount);
        metrics.SuccessfulOperations.Should().Be(operationCount);
    }
}
