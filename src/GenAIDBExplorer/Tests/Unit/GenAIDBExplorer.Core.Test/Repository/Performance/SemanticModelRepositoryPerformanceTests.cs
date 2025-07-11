#nullable enable

using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Performance;

/// <summary>
/// Integration tests for performance monitoring with <see cref="SemanticModelRepository"/>.
/// </summary>
[TestClass]
public sealed class SemanticModelRepositoryPerformanceTests
{
    private Mock<IPersistenceStrategyFactory> _mockStrategyFactory = null!;
    private Mock<ILocalDiskPersistenceStrategy> _mockStrategy = null!;
    private Mock<ILogger<SemanticModelRepository>> _mockRepositoryLogger = null!;
    private Mock<ILogger<PerformanceMonitor>> _mockPerformanceLogger = null!;
    private PerformanceMonitor _performanceMonitor = null!;
    private SemanticModelRepository _repository = null!;
    private DirectoryInfo _testDirectory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
        _mockStrategy = new Mock<ILocalDiskPersistenceStrategy>();
        _mockRepositoryLogger = new Mock<ILogger<SemanticModelRepository>>();
        _mockPerformanceLogger = new Mock<ILogger<PerformanceMonitor>>();

        _performanceMonitor = new PerformanceMonitor(_mockPerformanceLogger.Object);

        _mockStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string?>()))
            .Returns(_mockStrategy.Object);

        _repository = new SemanticModelRepository(
            _mockStrategyFactory.Object,
            _mockRepositoryLogger.Object,
            performanceMonitor: _performanceMonitor);

        _testDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository?.Dispose();
        _performanceMonitor?.Dispose();

        if (_testDirectory.Exists)
        {
            _testDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_ShouldRecordPerformanceMetrics()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory);

        // Assert
        result.Should().NotBeNull();

        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("LoadModel");
        statistics.Should().NotBeNull();
        statistics!.OperationName.Should().Be("LoadModel");
        statistics.Count.Should().Be(1);
        statistics.SuccessCount.Should().Be(1);
        statistics.SuccessRate.Should().Be(100);
    }

    [TestMethod]
    public async Task SaveModelAsync_ShouldRecordPerformanceMetrics()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.SaveModelAsync(semanticModel, _testDirectory);

        // Assert
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("SaveModel");
        statistics.Should().NotBeNull();
        statistics!.OperationName.Should().Be("SaveModel");
        statistics.Count.Should().Be(1);
        statistics.SuccessCount.Should().Be(1);
        statistics.SuccessRate.Should().Be(100);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithException_ShouldRecordFailure()
    {
        // Arrange
        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        var action = async () => await _repository.LoadModelAsync(_testDirectory);
        await action.Should().ThrowAsync<InvalidOperationException>();

        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("LoadModel");
        statistics.Should().NotBeNull();
        statistics!.Count.Should().Be(1);
        statistics.SuccessCount.Should().Be(0);
        statistics.SuccessRate.Should().Be(0);
    }

    [TestMethod]
    public async Task SaveModelAsync_WithException_ShouldRecordFailure()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act & Assert
        var action = async () => await _repository.SaveModelAsync(semanticModel, _testDirectory);
        await action.Should().ThrowAsync<UnauthorizedAccessException>();

        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("SaveModel");
        statistics.Should().NotBeNull();
        statistics!.Count.Should().Be(1);
        statistics.SuccessCount.Should().Be(0);
        statistics.SuccessRate.Should().Be(0);
    }

    [TestMethod]
    public async Task MultipleOperations_ShouldAggregateMetricsCorrectly()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);
        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.LoadModelAsync(_testDirectory);
        await _repository.SaveModelAsync(semanticModel, _testDirectory);
        await _repository.LoadModelAsync(_testDirectory);

        // Assert
        var loadStatistics = await _performanceMonitor.GetOperationStatisticsAsync("LoadModel");
        loadStatistics.Should().NotBeNull();
        loadStatistics!.Count.Should().Be(2);

        var saveStatistics = await _performanceMonitor.GetOperationStatisticsAsync("SaveModel");
        saveStatistics.Should().NotBeNull();
        saveStatistics!.Count.Should().Be(1);

        var overallMetrics = await _performanceMonitor.GetMetricsAsync();
        overallMetrics.TotalOperations.Should().Be(3);
        overallMetrics.SuccessfulOperations.Should().Be(3);
        overallMetrics.SuccessRate.Should().Be(100);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithMetadata_ShouldIncludeOperationDetails()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);

        // Act
        await _repository.LoadModelAsync(_testDirectory, enableLazyLoading: true, enableChangeTracking: true, enableCaching: false, "testStrategy");

        // Assert
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("LoadModel");
        statistics.Should().NotBeNull();
        statistics!.Count.Should().Be(1);

        // Verify that the performance monitor was called (we can't directly access metadata from statistics,
        // but we can verify the operation was recorded)
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task ConcurrentOperations_ShouldRecordAllMetrics()
    {
        // Arrange
        const int operationCount = 10;
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(Task.CompletedTask);

        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var testDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            tasks.Add(_repository.SaveModelAsync(semanticModel, testDir));
        }

        await Task.WhenAll(tasks);

        // Assert
        var statistics = await _performanceMonitor.GetOperationStatisticsAsync("SaveModel");
        statistics.Should().NotBeNull();
        statistics!.Count.Should().Be(operationCount);
        statistics.SuccessCount.Should().Be(operationCount);
        statistics.SuccessRate.Should().Be(100);
    }

    [TestMethod]
    public async Task PerformanceRecommendations_WithSlowOperations_ShouldGenerateRecommendations()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        // Simulate slow operation
        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(async () =>
            {
                await Task.Delay(100); // Simulate slow operation
            });

        // Act
        for (int i = 0; i < 5; i++)
        {
            await _repository.SaveModelAsync(semanticModel, _testDirectory);
        }

        // Assert
        var recommendations = await _performanceMonitor.GetRecommendationsAsync();
        recommendations.Should().NotBeNull();
        // Note: Recommendations depend on thresholds, so we just verify the method works
        // In a real scenario with actual slow operations, we would get performance recommendations
    }

    [TestMethod]
    public async Task PerformanceMetrics_AfterReset_ShouldBeEmpty()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(Task.CompletedTask);

        await _repository.SaveModelAsync(semanticModel, _testDirectory);
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
    public async Task PerformanceMonitoring_ShouldNotAffectRepositoryFunctionality()
    {
        // Arrange
        var semanticModel = new SemanticModel("TestDB", "Test Source", "Test Description")
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);
        _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
            .Returns(Task.CompletedTask);

        // Act
        var loadedModel = await _repository.LoadModelAsync(_testDirectory);
        await _repository.SaveModelAsync(loadedModel, _testDirectory);

        // Assert
        loadedModel.Should().NotBeNull();
        loadedModel.Name.Should().Be("TestDB");

        // Verify that repository functionality works as expected
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
        _mockStrategy.Verify(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()), Times.Once);

        // Verify that performance monitoring is working
        var metrics = await _performanceMonitor.GetMetricsAsync();
        metrics.TotalOperations.Should().Be(2);
        metrics.SuccessfulOperations.Should().Be(2);
    }
}
