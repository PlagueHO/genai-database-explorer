using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel.Lazy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel.Lazy;

/// <summary>
/// Unit tests for the LazyLoadingProxy class.
/// </summary>
[TestClass]
public class LazyLoadingProxyTests
{
    private Mock<ILogger<LazyLoadingProxy<string>>>? _mockLogger;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<LazyLoadingProxy<string>>>();
    }

    [TestMethod]
    public void Constructor_WithValidLoadFunction_ShouldCreateProxy()
    {
        // Arrange
        Func<Task<IEnumerable<string>>> loadFunction = () => Task.FromResult(Enumerable.Empty<string>());

        // Act
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Assert
        proxy.Should().NotBeNull();
        proxy.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithNullLoadFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<Task<IEnumerable<string>>>? loadFunction = null;

        // Act & Assert
        var action = () => new LazyLoadingProxy<string>(loadFunction!, _mockLogger?.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("loadFunction");
    }

    [TestMethod]
    public async Task GetEntitiesAsync_WhenNotLoaded_ShouldLoadAndReturnEntities()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var loadFunction = () => Task.FromResult<IEnumerable<string>>(expectedEntities);
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        var entities = await proxy.GetEntitiesAsync();

        // Assert
        entities.Should().BeEquivalentTo(expectedEntities);
        proxy.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetEntitiesAsync_WhenAlreadyLoaded_ShouldReturnCachedEntities()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var callCount = 0;
        var loadFunction = () =>
        {
            callCount++;
            return Task.FromResult<IEnumerable<string>>(expectedEntities);
        };
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        var firstCall = await proxy.GetEntitiesAsync();
        var secondCall = await proxy.GetEntitiesAsync();

        // Assert
        firstCall.Should().BeEquivalentTo(expectedEntities);
        secondCall.Should().BeEquivalentTo(expectedEntities);
        callCount.Should().Be(1, "load function should only be called once");
        proxy.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public async Task LoadAsync_WhenNotLoaded_ShouldLoadEntities()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var loadFunction = () => Task.FromResult<IEnumerable<string>>(expectedEntities);
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        await proxy.LoadAsync();

        // Assert
        proxy.IsLoaded.Should().BeTrue();
        var entities = await proxy.GetEntitiesAsync();
        entities.Should().BeEquivalentTo(expectedEntities);
    }

    [TestMethod]
    public async Task LoadAsync_WhenAlreadyLoaded_ShouldNotLoadAgain()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var callCount = 0;
        var loadFunction = () =>
        {
            callCount++;
            return Task.FromResult<IEnumerable<string>>(expectedEntities);
        };
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        await proxy.LoadAsync();
        await proxy.LoadAsync(); // Second call should not trigger loading

        // Assert
        callCount.Should().Be(1, "load function should only be called once");
        proxy.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public async Task LoadAsync_WhenLoadFunctionThrows_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Load failed");
        var loadFunction = () => Task.FromException<IEnumerable<string>>(expectedException);
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act & Assert
        var action = async () => await proxy.LoadAsync();
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Load failed");
        proxy.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void Reset_WhenLoaded_ShouldResetToUnloadedState()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var loadFunction = () => Task.FromResult<IEnumerable<string>>(expectedEntities);
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        proxy.LoadAsync().Wait(); // Load first
        proxy.Reset(); // Then reset

        // Assert
        proxy.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public async Task Reset_AfterReset_ShouldAllowReloading()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var callCount = 0;
        var loadFunction = () =>
        {
            callCount++;
            return Task.FromResult<IEnumerable<string>>(expectedEntities);
        };
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        await proxy.LoadAsync(); // Load first
        proxy.Reset(); // Reset
        await proxy.LoadAsync(); // Load again

        // Assert
        callCount.Should().Be(2, "load function should be called twice after reset");
        proxy.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var loadFunction = () => Task.FromResult(Enumerable.Empty<string>());
        var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act
        proxy.Dispose();

        // Assert - Should not throw when calling methods after disposal
        var action = async () => await proxy.GetEntitiesAsync();
        action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [TestMethod]
    public void DisposedProxy_MethodCalls_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var loadFunction = () => Task.FromResult(Enumerable.Empty<string>());
        var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);
        proxy.Dispose();

        // Act & Assert
        var getEntitiesAction = async () => await proxy.GetEntitiesAsync();
        getEntitiesAction.Should().ThrowAsync<ObjectDisposedException>();

        var loadAction = async () => await proxy.LoadAsync();
        loadAction.Should().ThrowAsync<ObjectDisposedException>();

        var resetAction = () => proxy.Reset();
        resetAction.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task ConcurrentAccess_ShouldLoadOnlyOnce()
    {
        // Arrange
        var expectedEntities = new[] { "entity1", "entity2", "entity3" };
        var callCount = 0;
        var loadFunction = async () =>
        {
            callCount++;
            await Task.Delay(100); // Simulate some work
            return (IEnumerable<string>)expectedEntities;
        };
        using var proxy = new LazyLoadingProxy<string>(loadFunction, _mockLogger?.Object);

        // Act - Start multiple concurrent operations
        var tasks = new[]
        {
            proxy.GetEntitiesAsync(),
            proxy.GetEntitiesAsync(),
            proxy.GetEntitiesAsync(),
            proxy.LoadAsync().ContinueWith(_ => proxy.GetEntitiesAsync()).Unwrap()
        };

        var results = await Task.WhenAll(tasks);

        // Assert
        callCount.Should().Be(1, "load function should only be called once despite concurrent access");
        proxy.IsLoaded.Should().BeTrue();
        
        foreach (var result in results)
        {
            result.Should().BeEquivalentTo(expectedEntities);
        }
    }
}
