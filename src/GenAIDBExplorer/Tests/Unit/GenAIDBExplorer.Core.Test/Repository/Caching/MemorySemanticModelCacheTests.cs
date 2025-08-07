using System;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Caching;

/// <summary>
/// Unit tests for MemorySemanticModelCache implementation.
/// </summary>
[TestClass]
public class MemorySemanticModelCacheTests
{
    private Mock<ILogger<MemorySemanticModelCache>> _mockLogger = null!;
    private IMemoryCache _memoryCache = null!;
    private CacheOptions _cacheOptions = null!;
    private MemorySemanticModelCache _cache = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange
        _mockLogger = new Mock<ILogger<MemorySemanticModelCache>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheOptions = new CacheOptions
        {
            Enabled = true,
            MaxCacheSize = 100,
            DefaultExpiration = TimeSpan.FromMinutes(30),
            MemoryLimitMB = 512,
            CompactionInterval = TimeSpan.FromMinutes(5),
            HitRateThreshold = 0.7,
            EnableStatistics = true
        };

        var options = new Mock<IOptions<CacheOptions>>();
        options.Setup(x => x.Value).Returns(_cacheOptions);

        _cache = new MemorySemanticModelCache(_memoryCache, options.Object, _mockLogger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _cache?.Dispose();
        _memoryCache?.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_WithValidKey_ReturnsNull_WhenCacheEmpty()
    {
        // Arrange
        var cacheKey = "test-key";

        // Act
        var result = await _cache.GetAsync(cacheKey);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        string cacheKey = null!;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _cache.GetAsync(cacheKey));
    }

    [TestMethod]
    public async Task GetAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var cacheKey = string.Empty;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cache.GetAsync(cacheKey));
    }

    [TestMethod]
    public async Task SetAsync_WithValidModel_StoresSuccessfully()
    {
        // Arrange
        var cacheKey = "test-key";
        var model = CreateTestSemanticModel();

        // Act
        await _cache.SetAsync(cacheKey, model);

        // Assert
        var retrieved = await _cache.GetAsync(cacheKey);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(model.Name);
    }

    [TestMethod]
    public async Task SetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        string cacheKey = null!;
        var model = CreateTestSemanticModel();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _cache.SetAsync(cacheKey, model));
    }

    [TestMethod]
    public async Task SetAsync_WithNullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var cacheKey = "test-key";
        SemanticModel model = null!;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _cache.SetAsync(cacheKey, model));
    }

    [TestMethod]
    public async Task SetAsync_WithCustomExpiration_UsesCustomExpiration()
    {
        // Arrange
        var cacheKey = "test-key";
        var model = CreateTestSemanticModel();
        var customExpiration = TimeSpan.FromMinutes(10);

        // Act
        await _cache.SetAsync(cacheKey, model, customExpiration);

        // Assert
        var retrieved = await _cache.GetAsync(cacheKey);
        retrieved.Should().NotBeNull();
    }

    [TestMethod]
    public async Task RemoveAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var cacheKey = "test-key";
        var model = CreateTestSemanticModel();
        await _cache.SetAsync(cacheKey, model);

        // Act
        var result = await _cache.RemoveAsync(cacheKey);

        // Assert
        result.Should().BeTrue();
        var retrieved = await _cache.GetAsync(cacheKey);
        retrieved.Should().BeNull();
    }

    [TestMethod]
    public async Task RemoveAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var cacheKey = "non-existent-key";

        // Act
        var result = await _cache.RemoveAsync(cacheKey);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        string cacheKey = null!;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _cache.RemoveAsync(cacheKey));
    }

    [TestMethod]
    public async Task ClearAsync_RemovesAllCachedItems()
    {
        // Arrange
        var model1 = CreateTestSemanticModel("Model1");
        var model2 = CreateTestSemanticModel("Model2");
        await _cache.SetAsync("key1", model1);
        await _cache.SetAsync("key2", model2);

        // Act
        await _cache.ClearAsync();

        // Assert
        var result1 = await _cache.GetAsync("key1");
        var result2 = await _cache.GetAsync("key2");
        result1.Should().BeNull();
        result2.Should().BeNull();
    }

    [TestMethod]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var model = CreateTestSemanticModel();
        await _cache.SetAsync("key1", model);
        
        // Trigger cache hit
        await _cache.GetAsync("key1");
        
        // Trigger cache miss
        await _cache.GetAsync("non-existent-key");

        // Act
        var statistics = await _cache.GetStatisticsAsync();

        // Assert
        statistics.Should().NotBeNull();
        statistics.TotalRequests.Should().Be(2);
        statistics.CacheHits.Should().Be(1);
        statistics.CacheMisses.Should().Be(1);
        statistics.HitRate.Should().Be(0.5);
        statistics.CacheSize.Should().Be(1);
        statistics.TotalMemoryUsage.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var cacheKey = "test-key";
        var model = CreateTestSemanticModel();
        await _cache.SetAsync(cacheKey, model);

        // Act
        var result = await _cache.ExistsAsync(cacheKey);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task ExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var cacheKey = "non-existent-key";

        // Act
        var result = await _cache.ExistsAsync(cacheKey);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        string cacheKey = null!;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _cache.ExistsAsync(cacheKey));
    }

    [TestMethod]
    public async Task GetAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cacheKey = "test-key";
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.GetAsync(cacheKey));
    }

    [TestMethod]
    public async Task SetAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cacheKey = "test-key";
        var model = CreateTestSemanticModel();
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.SetAsync(cacheKey, model));
    }

    [TestMethod]
    public async Task RemoveAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cacheKey = "test-key";
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.RemoveAsync(cacheKey));
    }

    [TestMethod]
    public async Task ClearAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.ClearAsync());
    }

    [TestMethod]
    public async Task GetStatisticsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.GetStatisticsAsync());
    }

    [TestMethod]
    public async Task ExistsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cacheKey = "test-key";
        _cache.Dispose();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _cache.ExistsAsync(cacheKey));
    }

    [TestMethod]
    public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
    {
        // Arrange
        IMemoryCache memoryCache = null!;
        var options = new Mock<IOptions<CacheOptions>>();
        options.Setup(x => x.Value).Returns(_cacheOptions);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new MemorySemanticModelCache(memoryCache, options.Object, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<CacheOptions> options = null!;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new MemorySemanticModelCache(_memoryCache, options, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new Mock<IOptions<CacheOptions>>();
        options.Setup(x => x.Value).Returns(_cacheOptions);
        ILogger<MemorySemanticModelCache> logger = null!;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new MemorySemanticModelCache(_memoryCache, options.Object, logger));
    }

    [TestMethod]
    public async Task GetStatisticsAsync_WithStatisticsDisabled_ReturnsZeroStatistics()
    {
        // Arrange
        _cacheOptions.EnableStatistics = false;
        var options = new Mock<IOptions<CacheOptions>>();
        options.Setup(x => x.Value).Returns(_cacheOptions);
        
        using var cache = new MemorySemanticModelCache(_memoryCache, options.Object, _mockLogger.Object);
        var model = CreateTestSemanticModel();
        await cache.SetAsync("key1", model);
        await cache.GetAsync("key1");

        // Act
        var statistics = await cache.GetStatisticsAsync();

        // Assert
        statistics.TotalRequests.Should().Be(0);
        statistics.CacheHits.Should().Be(0);
        statistics.CacheMisses.Should().Be(0);
        statistics.HitRate.Should().Be(0.0);
    }

    [TestMethod]
    public async Task Cache_WithMultipleOperations_MaintainsConsistentState()
    {
        // Arrange
        var model1 = CreateTestSemanticModel("Model1");
        var model2 = CreateTestSemanticModel("Model2");
        var model3 = CreateTestSemanticModel("Model3");

        // Act
        await _cache.SetAsync("key1", model1);
        await _cache.SetAsync("key2", model2);
        await _cache.SetAsync("key3", model3);

        var result1 = await _cache.GetAsync("key1");
        var result2 = await _cache.GetAsync("key2");
        var result3 = await _cache.GetAsync("key3");

        await _cache.RemoveAsync("key2");
        var removedResult = await _cache.GetAsync("key2");

        var statistics = await _cache.GetStatisticsAsync();

        // Assert
        result1.Should().NotBeNull();
        result1!.Name.Should().Be("Model1");
        result2.Should().NotBeNull();
        result2!.Name.Should().Be("Model2");
        result3.Should().NotBeNull();
        result3!.Name.Should().Be("Model3");
        removedResult.Should().BeNull();

        statistics.TotalRequests.Should().Be(4); // 3 successful gets + 1 miss
        statistics.CacheHits.Should().Be(3);
        statistics.CacheMisses.Should().Be(1);
        statistics.CacheSize.Should().Be(2); // 2 remaining items
    }

    /// <summary>
    /// Creates a test semantic model for testing purposes.
    /// </summary>
    /// <param name="name">Optional name for the model.</param>
    /// <returns>A test semantic model instance.</returns>
    private static SemanticModel CreateTestSemanticModel(string name = "TestModel")
    {
        return new SemanticModel(name, "Test model for unit testing", null)
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };
    }
}
