using FluentAssertions;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Api.Test.Services;

[TestClass]
public class SemanticModelCacheServiceTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISemanticModelRepository> _mockRepository = null!;
    private Mock<ILogger<SemanticModelCacheService>> _mockLogger = null!;
    private SemanticModelCacheService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProject = new Mock<IProject>();
        _mockRepository = new Mock<ISemanticModelRepository>();
        _mockLogger = new Mock<ILogger<SemanticModelCacheService>>();

        var projectDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "test-project"));
        _mockProject.Setup(p => p.ProjectDirectory).Returns(projectDir);
        _mockProject.Setup(p => p.Settings).Returns(new ProjectSettings
        {
            Database = new DatabaseSettings(),
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration { Directory = "semantic-model" }
            }
        });

        _sut = new SemanticModelCacheService(
            _mockProject.Object,
            _mockRepository.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public void IsLoaded_WhenNoModelLoaded_ShouldReturnFalse()
    {
        // Act & Assert
        _sut.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetModelAsync_ShouldLoadAndReturnModel()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _sut.GetModelAsync();

        // Assert
        result.Should().BeSameAs(expectedModel);
        _sut.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetModelAsync_WhenCalledTwice_ShouldOnlyLoadOnce()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ReturnsAsync(expectedModel);

        // Act
        await _sut.GetModelAsync();
        await _sut.GetModelAsync();

        // Assert
        _mockRepository.Verify(
            r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ReloadModelAsync_ShouldLoadFreshModel()
    {
        // Arrange
        var firstModel = CreateTestModel("FirstModel");
        var secondModel = CreateTestModel("SecondModel");
        var callCount = 0;
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ReturnsAsync(() => ++callCount == 1 ? firstModel : secondModel);

        // Act
        var first = await _sut.GetModelAsync();
        var second = await _sut.ReloadModelAsync();

        // Assert
        first.Should().BeSameAs(firstModel);
        second.Should().BeSameAs(secondModel);
        _mockRepository.Verify(
            r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task GetModelAsync_AfterReload_ShouldReturnReloadedModel()
    {
        // Arrange
        var firstModel = CreateTestModel("FirstModel");
        var secondModel = CreateTestModel("SecondModel");
        var callCount = 0;
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ReturnsAsync(() => ++callCount == 1 ? firstModel : secondModel);

        // Act
        await _sut.GetModelAsync();
        await _sut.ReloadModelAsync();
        var result = await _sut.GetModelAsync();

        // Assert
        result.Should().BeSameAs(secondModel);
        // Should still only be 2 calls (initial load + reload), not 3
        _mockRepository.Verify(
            r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task GetModelAsync_WhenLoadFails_ShouldPropagateException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ThrowsAsync(new FileNotFoundException("Model not found"));

        // Act
        var act = async () => await _sut.GetModelAsync();

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
        _sut.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetModelAsync_ConcurrentCalls_ShouldOnlyLoadOnce()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        var loadCount = 0;
        _mockRepository
            .Setup(r => r.LoadModelAsync(It.IsAny<DirectoryInfo>(), It.IsAny<SemanticModelRepositoryOptions>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref loadCount);
                return expectedModel;
            });

        // Act — fire multiple concurrent requests
        var tasks = Enumerable.Range(0, 10).Select(_ => _sut.GetModelAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert — all should get the same model, and load should happen once
        results.Should().AllSatisfy(r => r.Should().BeSameAs(expectedModel));
        loadCount.Should().Be(1);
    }

    private static SemanticModel CreateTestModel(string name = "TestModel")
    {
        var model = new SemanticModel(name, "TestSource", "Test description");
        model.AddTable(new SemanticModelTable("SalesLT", "Product", "Product table"));
        model.AddView(new SemanticModelView("SalesLT", "vProductAndDescription", "Product view"));
        model.AddStoredProcedure(new SemanticModelStoredProcedure("dbo", "uspGetCustomers", "SELECT 1", null, "Get customers"));
        return model;
    }
}
