using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Caching;

/// <summary>
/// Integration tests for SemanticModelRepository with caching functionality.
/// </summary>
[TestClass]
public class SemanticModelRepositoryCachingIntegrationTests
{
    private Mock<IPersistenceStrategyFactory> _mockStrategyFactory = null!;
    private Mock<ISemanticModelPersistenceStrategy> _mockStrategy = null!;
    private Mock<ISemanticModelCache> _mockCache = null!;
    private Mock<ILogger<SemanticModelRepository>> _mockLogger = null!;
    private Mock<ILoggerFactory> _mockLoggerFactory = null!;
    private SemanticModelRepository _repository = null!;
    private DirectoryInfo _testDirectory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange
        _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
        _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
        _mockCache = new Mock<ISemanticModelCache>();
        _mockLogger = new Mock<ILogger<SemanticModelRepository>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();

        _mockStrategyFactory.Setup(x => x.GetStrategy(It.IsAny<string>()))
            .Returns(_mockStrategy.Object);

        _testDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}"));
        _testDirectory.Create();

        _repository = new SemanticModelRepository(
            _mockStrategyFactory.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockCache.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository?.Dispose();
        
        if (_testDirectory?.Exists == true)
        {
            _testDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabled_ChecksCacheFirst()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        var cacheKey = "semantic_model_test_f9a7c2d1e4b6a8c9";

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(model);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(model.Name);

        // Verify cache was checked
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        
        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabled_LoadsFromPersistenceWhenCacheMiss()
    {
        // Arrange
        var model = CreateTestSemanticModel();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((SemanticModel?)null);

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, false, false, true);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(model.Name);

        // Verify cache was checked
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        
        // Verify persistence strategy was called due to cache miss
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
        
        // Verify model was stored in cache after loading
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), model, null), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingDisabled_SkipsCache()
    {
        // Arrange
        var model = CreateTestSemanticModel();

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, false, false, false);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(model.Name);

        // Verify cache was never accessed
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<SemanticModel>(), It.IsAny<TimeSpan?>()), Times.Never);
        
        // Verify persistence strategy was called directly
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabledAndLazyLoading_AppliesLazyLoadingToCachedModel()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, true, false, true);

        // Assert
        result.Should().NotBeNull();
        
        // Verify cache was checked
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        
        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabledAndChangeTracking_AppliesChangeTrackingToCachedModel()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, false, true, true);

        // Assert
        result.Should().NotBeNull();
        
        // Verify cache was checked
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        
        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WhenCacheThrowsException_ContinuesWithoutCaching()
    {
        // Arrange
        var model = CreateTestSemanticModel();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, false, false, true);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(model.Name);

        // Verify cache was attempted
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        
        // Verify persistence strategy was called due to cache error
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WhenCacheSetFails_ContinuesWithoutCaching()
    {
        // Arrange
        var model = CreateTestSemanticModel();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((SemanticModel?)null);

        _mockCache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<SemanticModel>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Cache set error"));

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act
        var result = await _repository.LoadModelAsync(_testDirectory, false, false, true);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(model.Name);

        // Verify cache operations were attempted
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), model, null), Times.Once);
        
        // Verify persistence strategy was called
        _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithNullCache_WorksWithoutCaching()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        
        var repositoryWithoutCache = new SemanticModelRepository(
            _mockStrategyFactory.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            cache: null);

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        try
        {
            // Act
            var result = await repositoryWithoutCache.LoadModelAsync(_testDirectory, false, false, true);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(model.Name);

            // Verify persistence strategy was called directly
            _mockStrategy.Verify(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
        }
        finally
        {
            repositoryWithoutCache.Dispose();
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_GeneratesConsistentCacheKeys()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        var capturedCacheKeys = new List<string>();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(key => capturedCacheKeys.Add(key))
            .ReturnsAsync((SemanticModel?)null);

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act - Load same model twice
        await _repository.LoadModelAsync(_testDirectory, false, false, true);
        await _repository.LoadModelAsync(_testDirectory, false, false, true);

        // Assert
        capturedCacheKeys.Should().HaveCount(2);
        capturedCacheKeys[0].Should().Be(capturedCacheKeys[1]);
        capturedCacheKeys[0].Should().StartWith("semantic_model_");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithDifferentStrategies_GeneratesDifferentCacheKeys()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        var capturedCacheKeys = new List<string>();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(key => capturedCacheKeys.Add(key))
            .ReturnsAsync((SemanticModel?)null);

        _mockStrategy.Setup(x => x.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(model);

        // Act - Load with different strategies
        await _repository.LoadModelAsync(_testDirectory, false, false, true, "strategy1");
        await _repository.LoadModelAsync(_testDirectory, false, false, true, "strategy2");

        // Assert
        capturedCacheKeys.Should().HaveCount(2);
        capturedCacheKeys[0].Should().NotBe(capturedCacheKeys[1]);
        capturedCacheKeys[0].Should().StartWith("semantic_model_");
        capturedCacheKeys[1].Should().StartWith("semantic_model_");
    }

    /// <summary>
    /// Creates a test semantic model for testing purposes.
    /// </summary>
    /// <returns>A test semantic model instance.</returns>
    private static SemanticModel CreateTestSemanticModel()
    {
        return new SemanticModel
        {
            Name = "TestModel",
            Description = "Test model for unit testing",
            Tables = [],
            Views = [],
            StoredProcedures = []
        };
    }
}
