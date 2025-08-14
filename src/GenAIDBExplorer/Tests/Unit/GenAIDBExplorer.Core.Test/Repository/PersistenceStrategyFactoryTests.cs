using FluentAssertions;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Repository;

/// <summary>
/// Unit tests for PersistenceStrategyFactory class.
/// Tests lazy loading behavior and strategy selection.
/// </summary>
[TestClass]
public class PersistenceStrategyFactoryTests
{
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILocalDiskPersistenceStrategy> _mockLocalDiskStrategy = null!;
    private Mock<IAzureBlobPersistenceStrategy> _mockAzureBlobStrategy = null!;
    private Mock<ICosmosDbPersistenceStrategy> _mockCosmosDbStrategy = null!;
    private PersistenceStrategyFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange - Create mocks
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLocalDiskStrategy = new Mock<ILocalDiskPersistenceStrategy>();
        _mockAzureBlobStrategy = new Mock<IAzureBlobPersistenceStrategy>();
        _mockCosmosDbStrategy = new Mock<ICosmosDbPersistenceStrategy>();

        // Setup default configuration to return LocalDisk
        _mockConfiguration.Setup(c => c["PersistenceStrategy"]).Returns((string?)null);

        _factory = new PersistenceStrategyFactory(_mockServiceProvider.Object, _mockConfiguration.Object);
    }

    [TestMethod]
    public void Constructor_ShouldNotInstantiateStrategies_OnCreation()
    {
        // Arrange & Act - Constructor is called in TestInitialize

        // Assert - Verify that strategies are not requested during construction
        _mockServiceProvider.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
        // Note: Can't verify GetService extension method with Moq, 
        // but we can verify that no service requests were made
    }

    [TestMethod]
    public void GetStrategy_ShouldReturnLocalDiskStrategy_WhenNoStrategySpecified()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)))
            .Returns(_mockLocalDiskStrategy.Object);

        // Act
        var result = _factory.GetStrategy();

        // Assert
        result.Should().BeSameAs(_mockLocalDiskStrategy.Object);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldReturnLocalDiskStrategy_WhenLocalDiskSpecified()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)))
            .Returns(_mockLocalDiskStrategy.Object);

        // Act
        var result = _factory.GetStrategy("LocalDisk");

        // Assert
        result.Should().BeSameAs(_mockLocalDiskStrategy.Object);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldReturnAzureBlobStrategy_WhenAzureBlobSpecified()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAzureBlobPersistenceStrategy)))
            .Returns(_mockAzureBlobStrategy.Object);

        // Act
        var result = _factory.GetStrategy("AzureBlob");

        // Assert
        result.Should().BeSameAs(_mockAzureBlobStrategy.Object);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(IAzureBlobPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldReturnCosmosStrategy_WhenCosmosSpecified()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICosmosDbPersistenceStrategy)))
            .Returns(_mockCosmosDbStrategy.Object);

        // Act
        var result = _factory.GetStrategy("CosmosDb");

        // Assert
        result.Should().BeSameAs(_mockCosmosDbStrategy.Object);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ICosmosDbPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldBeCaseInsensitive()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)))
            .Returns(_mockLocalDiskStrategy.Object);

        // Act
        var result1 = _factory.GetStrategy("localdisk");
        var result2 = _factory.GetStrategy("LOCALDISK");
        var result3 = _factory.GetStrategy("LocalDisk");

        // Assert
        result1.Should().BeSameAs(_mockLocalDiskStrategy.Object);
        result2.Should().BeSameAs(_mockLocalDiskStrategy.Object);
        result3.Should().BeSameAs(_mockLocalDiskStrategy.Object);
    }

    [TestMethod]
    public void GetStrategy_ShouldCacheStrategies_OnSecondCall()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)))
            .Returns(_mockLocalDiskStrategy.Object);

        // Act
        var result1 = _factory.GetStrategy("LocalDisk");
        var result2 = _factory.GetStrategy("LocalDisk");

        // Assert
        result1.Should().BeSameAs(result2);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldUseConfigurationDefault_WhenNoStrategySpecified()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["PersistenceStrategy"]).Returns("AzureBlob");
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAzureBlobPersistenceStrategy)))
            .Returns(_mockAzureBlobStrategy.Object);

        var factory = new PersistenceStrategyFactory(_mockServiceProvider.Object, _mockConfiguration.Object);

        // Act
        var result = factory.GetStrategy();

        // Assert
        result.Should().BeSameAs(_mockAzureBlobStrategy.Object);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(IAzureBlobPersistenceStrategy)), Times.Once);
    }

    [TestMethod]
    public void GetStrategy_ShouldThrowException_WhenUnsupportedStrategySpecified()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() => _factory.GetStrategy("UnsupportedStrategy"));
    }

    [TestMethod]
    public void GetStrategy_ShouldNotRequestOtherStrategies_WhenOnlyOneIsUsed()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)))
            .Returns(_mockLocalDiskStrategy.Object);

        // Act
        var result = _factory.GetStrategy("LocalDisk");

        // Assert
        result.Should().BeSameAs(_mockLocalDiskStrategy.Object);

        // Verify that only LocalDisk strategy was requested
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ILocalDiskPersistenceStrategy)), Times.Once);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(IAzureBlobPersistenceStrategy)), Times.Never);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(ICosmosDbPersistenceStrategy)), Times.Never);
    }
}
