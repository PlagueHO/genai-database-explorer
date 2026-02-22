using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Caching;

/// <summary>
/// Unit tests for SemanticModelRepository caching functionality.
/// </summary>
[TestClass]
public class SemanticModelRepositoryCachingTests
{
    private Mock<IPersistenceStrategyFactory>? _mockStrategyFactory;
    private Mock<ISemanticModelPersistenceStrategy>? _mockStrategy;
    private Mock<ISemanticModelCache>? _mockCache;
    private Mock<ILogger<SemanticModelRepository>>? _mockLogger;
    private Mock<ILoggerFactory>? _mockLoggerFactory;
    private Mock<ILogger<ChangeTracker>>? _mockChangeTrackerLogger;
    private SemanticModelRepository? _repository;
    private DirectoryInfo? _testModelPath;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
        _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
        _mockCache = new Mock<ISemanticModelCache>();
        _mockLogger = new Mock<ILogger<SemanticModelRepository>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockChangeTrackerLogger = new Mock<ILogger<ChangeTracker>>();

        _mockStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string?>()))
            .Returns(_mockStrategy.Object);

        _mockLoggerFactory.Setup(f => f.CreateLogger(typeof(ChangeTracker).FullName!))
            .Returns(_mockChangeTrackerLogger.Object);

        _repository = new SemanticModelRepository(
            _mockStrategyFactory.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockCache.Object);

        _testModelPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository?.Dispose();

        if (_testModelPath?.Exists == true)
        {
            _testModelPath.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabled_ChecksCacheFirst()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedModel.Name);

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy!.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabled_LoadsFromPersistenceOnCacheMiss()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((SemanticModel?)null);
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedModel.Name);

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was called due to cache miss
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);

        // Verify model was stored in cache after loading
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), expectedModel, null), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingDisabled_SkipsCache()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .Build(); // Default is caching disabled

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedModel.Name);

        // Verify cache was never accessed
        _mockCache!.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Never);
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SemanticModel>(), It.IsAny<TimeSpan?>()), Times.Never);

        // Verify persistence strategy was called directly
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabledAndLazyLoading_AppliesLazyLoadingToCachedModel()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.IsLazyLoadingEnabled.Should().BeTrue();

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy!.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabledAndChangeTracking_AppliesChangeTrackingToCachedModel()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithChangeTracking()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.IsChangeTrackingEnabled.Should().BeTrue();

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy!.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCachingEnabledAndBothFeatures_AppliesBothToCachedModel()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading()
            .WithChangeTracking()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.IsLazyLoadingEnabled.Should().BeTrue();
        result.IsChangeTrackingEnabled.Should().BeTrue();

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy!.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_WhenCacheGetThrowsException_ContinuesWithoutCaching()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedModel.Name);

        // Verify cache was attempted
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was called due to cache error
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WhenCacheSetFails_ContinuesWithoutCaching()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((SemanticModel?)null);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SemanticModel>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Cache set error"));
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedModel.Name);

        // Verify cache operations were attempted
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), expectedModel, null), Times.Once);

        // Verify persistence strategy was called
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithNullCache_WorksWithoutCaching()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        var repositoryWithoutCache = new SemanticModelRepository(
            _mockStrategyFactory!.Object,
            _mockLogger!.Object,
            _mockLoggerFactory!.Object,
            cache: null);

        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        try
        {
            // Act
            var result = await repositoryWithoutCache.LoadModelAsync(_testModelPath!, options);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(expectedModel.Name);

            // Verify persistence strategy was called directly
            _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
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
        var expectedModel = CreateTestSemanticModel();
        var capturedCacheKeys = new List<string>();

        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .Callback<string>(key => capturedCacheKeys.Add(key))
            .ReturnsAsync((SemanticModel?)null);
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .Build();

        // Act - Load same model twice
        await _repository!.LoadModelAsync(_testModelPath!, options);
        await _repository.LoadModelAsync(_testModelPath!, options);

        // Assert
        capturedCacheKeys.Should().HaveCount(2);
        capturedCacheKeys[0].Should().Be(capturedCacheKeys[1]);
        capturedCacheKeys[0].Should().StartWith("semantic_model_");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithDifferentStrategies_GeneratesDifferentCacheKeys()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        var capturedCacheKeys = new List<string>();

        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .Callback<string>(key => capturedCacheKeys.Add(key))
            .ReturnsAsync((SemanticModel?)null);
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options1 = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .WithStrategyName("strategy1")
            .Build();

        var options2 = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .WithStrategyName("strategy2")
            .Build();

        // Act - Load with different strategies
        await _repository!.LoadModelAsync(_testModelPath!, options1);
        await _repository.LoadModelAsync(_testModelPath!, options2);

        // Assert
        capturedCacheKeys.Should().HaveCount(2);
        capturedCacheKeys[0].Should().NotBe(capturedCacheKeys[1]);
        capturedCacheKeys[0].Should().StartWith("semantic_model_");
        capturedCacheKeys[1].Should().StartWith("semantic_model_");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithSameStrategyName_GeneratesSameCacheKey()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        var capturedCacheKeys = new List<string>();

        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .Callback<string>(key => capturedCacheKeys.Add(key))
            .ReturnsAsync((SemanticModel?)null);
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithCaching()
            .WithStrategyName("custom-strategy")
            .Build();

        // Act - Load with same strategy name
        await _repository!.LoadModelAsync(_testModelPath!, options);
        await _repository.LoadModelAsync(_testModelPath!, options);

        // Assert
        capturedCacheKeys.Should().HaveCount(2);
        capturedCacheKeys[0].Should().Be(capturedCacheKeys[1]);
        capturedCacheKeys[0].Should().StartWith("semantic_model_");
    }

    [TestMethod]
    public async Task LoadModelAsync_BackwardCompatibility_ShouldMaintainExistingBehavior()
    {
        // Arrange - Use simple repository constructor like the working lazy loading tests
        var simpleRepository = new SemanticModelRepository(_mockStrategyFactory!.Object, _mockLogger!.Object);

        var expectedModel1 = new SemanticModel("TestModel1", "TestSource1");
        var expectedModel2 = new SemanticModel("TestModel2", "TestSource2");

        _mockStrategy!.SetupSequence(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel1)
            .ReturnsAsync(expectedModel2);

        try
        {
            // Act - Use existing method overloads (without caching parameter)
            var result1 = await simpleRepository.LoadModelAsync(_testModelPath!);
            var result2 = await simpleRepository.LoadModelAsync(_testModelPath!, "strategyName");

            // Assert - Should work exactly as before (no caching)
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();

            // Verify cache was never accessed for backward compatibility calls
            _mockCache!.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Never);
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SemanticModel>(), It.IsAny<TimeSpan?>()), Times.Never);

            // Verify persistence strategy was called directly for all calls
            _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Exactly(2));
        }
        finally
        {
            simpleRepository.Dispose();
        }
    }

    [TestMethod]
    public async Task LoadModelAsync_CachedModelWithLazyLoadingAlreadyEnabled_ShouldNotReenableLazyLoading()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        expectedModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.IsLazyLoadingEnabled.Should().BeTrue();

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadModelAsync_CachedModelWithChangeTrackingAlreadyEnabled_ShouldNotReenableChangeTracking()
    {
        // Arrange
        var expectedModel = CreateTestSemanticModel();
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        expectedModel.EnableChangeTracking(changeTracker);

        _mockCache!.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedModel);

        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithChangeTracking()
            .WithCaching()
            .Build();

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, options);

        // Assert
        result.Should().NotBeNull();
        result.IsChangeTrackingEnabled.Should().BeTrue();

        // Verify cache was checked
        _mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);

        // Verify persistence strategy was NOT called since model was found in cache
        _mockStrategy!.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    /// <summary>
    /// Creates a test semantic model for testing purposes.
    /// </summary>
    /// <returns>A test semantic model instance.</returns>
    private static SemanticModel CreateTestSemanticModel()
    {
        return new SemanticModel("TestModel", "TestSource", "Test model for unit testing");
    }
}
