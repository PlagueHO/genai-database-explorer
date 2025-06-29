using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Repository;

/// <summary>
/// Unit tests for SemanticModelRepository lazy loading functionality.
/// </summary>
[TestClass]
public class SemanticModelRepositoryLazyLoadingTests
{
    private Mock<IPersistenceStrategyFactory>? _mockStrategyFactory;
    private Mock<ISemanticModelPersistenceStrategy>? _mockStrategy;
    private Mock<ILogger<SemanticModelRepository>>? _mockLogger;
    private SemanticModelRepository? _repository;
    private DirectoryInfo? _testModelPath;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
        _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
        _mockLogger = new Mock<ILogger<SemanticModelRepository>>();

        _mockStrategyFactory
            .Setup(f => f.GetStrategy(It.IsAny<string>()))
            .Returns(_mockStrategy.Object);

        _repository = new SemanticModelRepository(_mockStrategyFactory.Object, _mockLogger.Object);

        // Create a temporary directory for testing
        var tempPath = Path.Combine(Path.GetTempPath(), "SemanticModelRepositoryTests", Guid.NewGuid().ToString());
        _testModelPath = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(_testModelPath.FullName);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (_testModelPath?.Exists == true)
        {
            _testModelPath.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_WithoutLazyLoading_ShouldUseDefaultBehavior()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!);

        // Assert
        result.Should().BeSameAs(expectedModel);
        result.IsLazyLoadingEnabled.Should().BeFalse();
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithStrategyName_ShouldUseSpecifiedStrategy()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        const string strategyName = "custom-strategy";
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, strategyName);

        // Assert
        result.Should().BeSameAs(expectedModel);
        _mockStrategyFactory!.Verify(f => f.GetStrategy(strategyName), Times.Once);
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithLazyLoadingEnabled_ShouldEnableLazyLoading()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: true);

        // Assert
        result.Should().BeSameAs(expectedModel);
        result.IsLazyLoadingEnabled.Should().BeTrue();
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithLazyLoadingEnabledAndStrategyName_ShouldUseSpecifiedStrategyAndEnableLazyLoading()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        const string strategyName = "custom-strategy";
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: true, strategyName);

        // Assert
        result.Should().BeSameAs(expectedModel);
        result.IsLazyLoadingEnabled.Should().BeTrue();
        _mockStrategyFactory!.Verify(f => f.GetStrategy(strategyName), Times.Once);
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithLazyLoadingDisabled_ShouldNotEnableLazyLoading()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: false);

        // Assert
        result.Should().BeSameAs(expectedModel);
        result.IsLazyLoadingEnabled.Should().BeFalse();
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task SaveModelAsync_ShouldUseUnderlyingStrategy()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource");
        _mockStrategy!
            .Setup(s => s.SaveModelAsync(model, _testModelPath!))
            .Returns(Task.CompletedTask);

        // Act
        await _repository!.SaveModelAsync(model, _testModelPath!);

        // Assert
        _mockStrategy.Verify(s => s.SaveModelAsync(model, _testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task SaveModelAsync_WithStrategyName_ShouldUseSpecifiedStrategy()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource");
        const string strategyName = "custom-strategy";
        _mockStrategy!
            .Setup(s => s.SaveModelAsync(model, _testModelPath!))
            .Returns(Task.CompletedTask);

        // Act
        await _repository!.SaveModelAsync(model, _testModelPath!, strategyName);

        // Assert
        _mockStrategyFactory!.Verify(f => f.GetStrategy(strategyName), Times.Once);
        _mockStrategy.Verify(s => s.SaveModelAsync(model, _testModelPath), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_BackwardCompatibility_ShouldMaintainExistingBehavior()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource");
        _mockStrategy!
            .Setup(s => s.LoadModelAsync(_testModelPath!))
            .ReturnsAsync(expectedModel);

        // Act - Use the original method signature (without lazy loading parameter)
        var result = await _repository!.LoadModelAsync(_testModelPath!);

        // Assert - Should work exactly as before
        result.Should().BeSameAs(expectedModel);
        result.IsLazyLoadingEnabled.Should().BeFalse();
        _mockStrategy.Verify(s => s.LoadModelAsync(_testModelPath), Times.Once);
    }
}
