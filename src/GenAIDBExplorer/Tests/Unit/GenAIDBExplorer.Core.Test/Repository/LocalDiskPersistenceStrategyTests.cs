using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository;

[TestClass]
public class LocalDiskPersistenceStrategyTests
{
    private Mock<ILogger<LocalDiskPersistenceStrategy>>? _mockLogger;
    private LocalDiskPersistenceStrategy? _strategy;
    private DirectoryInfo? _testDirectory;
    private SemanticModel? _testModel;

    [TestInitialize]
    public void Setup()
    {
        // Arrange
        _mockLogger = new Mock<ILogger<LocalDiskPersistenceStrategy>>();
        _strategy = new LocalDiskPersistenceStrategy(_mockLogger.Object);

        // Create a unique test directory
        var tempPath = Path.Combine(Path.GetTempPath(), $"SemanticModelTests_{Guid.NewGuid():N}");
        _testDirectory = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(_testDirectory.FullName);

        // Create a test semantic model
        _testModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _testModel.AddTable(new SemanticModelTable("dbo", "TestTable"));
        _testModel.AddView(new SemanticModelView("dbo", "TestView"));
        _testModel.AddStoredProcedure(new SemanticModelStoredProcedure("dbo", "TestProcedure", "CREATE PROCEDURE dbo.TestProcedure AS BEGIN SELECT 1 END"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (_testDirectory?.Exists == true)
        {
            try
            {
                _testDirectory.Delete(true);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }

    [TestMethod]
    public async Task SaveModelAsync_ValidModel_SavesSuccessfully()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));

        // Act
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Assert
        modelPath.Exists.Should().BeTrue();
        File.Exists(Path.Combine(modelPath.FullName, "semanticmodel.json")).Should().BeTrue();
        File.Exists(Path.Combine(modelPath.FullName, "index.json")).Should().BeTrue();
        Directory.Exists(Path.Combine(modelPath.FullName, "tables")).Should().BeTrue();
        Directory.Exists(Path.Combine(modelPath.FullName, "views")).Should().BeTrue();
        Directory.Exists(Path.Combine(modelPath.FullName, "storedprocedures")).Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveModelAsync_NullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));

        // Act & Assert
        await _strategy!.Invoking(s => s.SaveModelAsync(null!, modelPath))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task SaveModelAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _strategy!.Invoking(s => s.SaveModelAsync(_testModel!, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task LoadModelAsync_ExistingModel_LoadsSuccessfully()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Act
        var loadedModel = await _strategy.LoadModelAsync(modelPath);

        // Assert
        loadedModel.Should().NotBeNull();
        loadedModel.Name.Should().Be(_testModel!.Name);
        loadedModel.Source.Should().Be(_testModel.Source);
        loadedModel.Description.Should().Be(_testModel.Description);
        loadedModel.Tables.Should().HaveCount(_testModel.Tables.Count);
        loadedModel.Views.Should().HaveCount(_testModel.Views.Count);
        loadedModel.StoredProcedures.Should().HaveCount(_testModel.StoredProcedures.Count);
    }

    [TestMethod]
    public async Task LoadModelAsync_NonExistentModel_ThrowsFileNotFoundException()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "NonExistentModel"));

        // Act & Assert
        await _strategy!.Invoking(s => s.LoadModelAsync(modelPath))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task LoadModelAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _strategy!.Invoking(s => s.LoadModelAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ExistsAsync_ExistingModel_ReturnsTrue()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Act
        var exists = await _strategy.ExistsAsync(modelPath);

        // Assert
        exists.Should().BeTrue();
    }

    [TestMethod]
    public async Task ExistsAsync_NonExistentModel_ReturnsFalse()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "NonExistentModel"));

        // Act
        var exists = await _strategy!.ExistsAsync(modelPath);

        // Assert
        exists.Should().BeFalse();
    }

    [TestMethod]
    public async Task ExistsAsync_DirectoryExistsButNoSemanticModel_ReturnsFalse()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "EmptyDirectory"));
        Directory.CreateDirectory(modelPath.FullName);

        // Act
        var exists = await _strategy!.ExistsAsync(modelPath);

        // Assert
        exists.Should().BeFalse();
    }

    [TestMethod]
    public async Task ListModelsAsync_MultipleModels_ReturnsAllModels()
    {
        // Arrange
        var model1Path = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "Model1"));
        var model2Path = new DirectoryInfo(Path.Combine(_testDirectory.FullName, "Model2"));

        await _strategy!.SaveModelAsync(_testModel!, model1Path);

        var secondModel = new SemanticModel("Model2", "Source2");
        await _strategy.SaveModelAsync(secondModel, model2Path);

        // Act
        var models = await _strategy.ListModelsAsync(_testDirectory);

        // Assert
        models.Should().HaveCount(2);
        models.Should().Contain("Model1");
        models.Should().Contain("Model2");
    }

    [TestMethod]
    public async Task ListModelsAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var models = await _strategy!.ListModelsAsync(_testDirectory!);

        // Assert
        models.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ListModelsAsync_NonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "NonExistent"));

        // Act
        var models = await _strategy!.ListModelsAsync(nonExistentPath);

        // Assert
        models.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DeleteModelAsync_ExistingModel_DeletesSuccessfully()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Verify it exists first
        modelPath.Refresh();
        modelPath.Exists.Should().BeTrue();

        // Act
        await _strategy.DeleteModelAsync(modelPath);

        // Assert
        modelPath.Refresh();
        modelPath.Exists.Should().BeFalse();
    }

    [TestMethod]
    public async Task DeleteModelAsync_NonExistentModel_DoesNotThrow()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "NonExistentModel"));

        // Act & Assert
        await _strategy!.Invoking(s => s.DeleteModelAsync(modelPath))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task DeleteModelAsync_DirectoryWithoutSemanticModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "EmptyDirectory"));
        Directory.CreateDirectory(modelPath.FullName);

        // Act & Assert
        await _strategy!.Invoking(s => s.DeleteModelAsync(modelPath))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not contain a semantic model*");
    }

    [TestMethod]
    public async Task SaveModelAsync_GeneratesIndexFile_WithCorrectStructure()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "TestModel"));

        // Act
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Assert
        var indexPath = Path.Combine(modelPath.FullName, "index.json");
        File.Exists(indexPath).Should().BeTrue();

        var indexContent = await File.ReadAllTextAsync(indexPath);
        indexContent.Should().Contain("TestModel");
        indexContent.Should().Contain("TestSource");
        indexContent.Should().Contain("tables");
        indexContent.Should().Contain("views");
        indexContent.Should().Contain("storedProcedures");
        indexContent.Should().Contain("TestTable");
        indexContent.Should().Contain("TestView");
        indexContent.Should().Contain("TestProcedure");
    }

    [TestMethod]
    public async Task SaveAndLoadAsync_PreservesBackwardCompatibility()
    {
        // Arrange
        var modelPath = new DirectoryInfo(Path.Combine(_testDirectory!.FullName, "CompatibilityTest"));

        // Act - Save using new strategy
        await _strategy!.SaveModelAsync(_testModel!, modelPath);

        // Load using original SemanticModel method
        var placeholder = new SemanticModel(string.Empty, string.Empty);
        var loadedModel = await placeholder.LoadModelAsync(modelPath);

        // Assert - Should load successfully with original method
        loadedModel.Should().NotBeNull();
        loadedModel.Name.Should().Be(_testModel!.Name);
        loadedModel.Source.Should().Be(_testModel.Source);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new LocalDiskPersistenceStrategy(null!));
    }
}
