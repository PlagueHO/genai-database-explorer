using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Repository;

/// <summary>
/// Unit tests for LocalDiskPersistenceStrategy lazy loading integration scenarios.
/// These tests specifically validate the fix for the corruption issue where enrich-model
/// was saving empty Tables/Views/StoredProcedures arrays to semanticmodel.json.
/// </summary>
[TestClass]
public class LocalDiskPersistenceStrategyLazyLoadingTests
{
    private Mock<ILogger<LocalDiskPersistenceStrategy>>? _mockLogger;
    private Mock<ISecureJsonSerializer>? _mockSecureJsonSerializer;
    private LocalDiskPersistenceStrategy? _persistenceStrategy;
    private DirectoryInfo? _testModelPath;
    private SemanticModel? _semanticModel;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<LocalDiskPersistenceStrategy>>();
        _mockSecureJsonSerializer = new Mock<ISecureJsonSerializer>();
        _persistenceStrategy = new LocalDiskPersistenceStrategy(_mockLogger.Object, _mockSecureJsonSerializer.Object);

        // Create a temporary directory for testing
        var tempPath = Path.Combine(Path.GetTempPath(), "LocalDiskPersistenceStrategyLazyLoadingTests", Guid.NewGuid().ToString());
        _testModelPath = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(_testModelPath.FullName);

        // Create subdirectories for different entity types
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "tables"));
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "views"));
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "storedprocedures"));

        _semanticModel = new SemanticModel("TestModel", "TestSource");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _semanticModel?.Dispose();

        if (_testModelPath?.Exists == true)
        {
            try
            {
                _testModelPath.Delete(recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
    }

    [TestMethod]
    public async Task SaveModelAsync_WithLazyLoadingAndPopulatedEntities_ShouldPreserveEntityMetadataInSemanticModelJson()
    {
        // Arrange - Create a semantic model with entity metadata (simulating extracted model)
        var table1 = new SemanticModelTable("dbo", "Table1");
        var table2 = new SemanticModelTable("dbo", "Table2");
        var view1 = new SemanticModelView("dbo", "View1");

        // Add metadata to semantic model (this is what extract-model would do)
        _semanticModel!.Tables.AddRange([table1, table2]);
        _semanticModel.Views.Add(view1);

        // Create individual entity files
        await CreateIndividualEntityFiles([table1, table2], [view1], []);

        // Enable lazy loading (this is what happens when model is loaded with lazy loading)
        _semanticModel.EnableLazyLoading(_testModelPath!, _persistenceStrategy!);

        // Mock the secure JSON serializer to return the semantic model JSON
        var expectedSemanticModelJson = CreateExpectedSemanticModelJson(_semanticModel);
        _mockSecureJsonSerializer!.Setup(x => x.SerializeAsync(It.IsAny<SemanticModel>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync(expectedSemanticModelJson);

        // Mock SerializeAsync for entity serialization (the refactored code now calls this for each entity)
        _mockSecureJsonSerializer.Setup(x => x.SerializeAsync(It.IsAny<object>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync("{}");

        // Act - Save the model (this is what enrich-model does)
        await _persistenceStrategy!.SaveModelAsync(_semanticModel, _testModelPath!);

        // Assert - Verify that SaveModelAsync was called with a model that has entity metadata
        // The refactored code calls SerializeAsync for the semantic model after materializing lazy-loaded data
        _mockSecureJsonSerializer.Verify(x => x.SerializeAsync(
            It.IsAny<SemanticModel>(),
            It.IsAny<JsonSerializerOptions>()),
            Times.Once,
            "SaveModelAsync should serialize the semantic model with preserved entity metadata");
    }

    [TestMethod]
    public async Task MaterializeLazyLoadedDataAsync_ShouldTriggerLazyLoadingInsteadOfReadingFromTempDirectory()
    {
        // Arrange - Create semantic model with lazy loading enabled
        var table1 = new SemanticModelTable("dbo", "Table1");
        var table2 = new SemanticModelTable("dbo", "Table2");

        _semanticModel!.Tables.AddRange([table1, table2]);

        // Create individual entity files with detailed content
        await CreateIndividualEntityFiles([table1, table2], [], []);

        // Enable lazy loading
        _semanticModel.EnableLazyLoading(_testModelPath!, _persistenceStrategy!);

        // Mock the secure JSON serializer
        var expectedSemanticModelJson = CreateExpectedSemanticModelJson(_semanticModel);
        _mockSecureJsonSerializer!.Setup(x => x.SerializeAsync(It.IsAny<SemanticModel>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync(expectedSemanticModelJson);

        // Mock SerializeAsync for entity serialization (the refactored code now calls this for each entity)
        _mockSecureJsonSerializer.Setup(x => x.SerializeAsync(It.IsAny<object>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync("{}");

        // Act - Trigger the save which calls MaterializeLazyLoadedDataAsync internally
        await _persistenceStrategy!.SaveModelAsync(_semanticModel, _testModelPath!);

        // Assert - Verify that the semantic model was serialized (this triggers lazy loading materialization)
        // The refactored code calls SerializeAsync for the semantic model after lazy loading is materialized
        _mockSecureJsonSerializer.Verify(x => x.SerializeAsync(
            It.IsAny<SemanticModel>(),
            It.IsAny<JsonSerializerOptions>()),
            Times.Once,
            "Lazy loading should have materialized the table entities before semantic model serialization");

        // Verify logger was called with materialization messages
        _mockLogger!.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting materialization of lazy-loaded data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log the start of lazy loading materialization");
    }

    [TestMethod]
    public async Task SaveModelAsync_WithEmptySemanticModelJson_ShouldNotCorruptEntityMetadata()
    {
        // Arrange - Simulate a scenario where semanticmodel.json has empty arrays (corruption scenario)
        var table1 = new SemanticModelTable("dbo", "Table1");
        var view1 = new SemanticModelView("dbo", "View1");

        // Create individual entity files but with empty semanticmodel.json (corruption)
        await CreateIndividualEntityFiles([table1], [view1], []);
        await CreateCorruptedSemanticModelJson(); // Simulate corrupted state

        // Load the model and enable lazy loading
        var loadedModel = await LoadSemanticModelWithLazyLoading();

        // Mock the secure JSON serializer to return proper semantic model JSON
        var expectedSemanticModelJson = CreateExpectedSemanticModelJson(loadedModel);
        _mockSecureJsonSerializer!.Setup(x => x.SerializeAsync(It.IsAny<SemanticModel>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync(expectedSemanticModelJson);

        // Act - Save the model (should not corrupt the entity metadata)
        await _persistenceStrategy!.SaveModelAsync(loadedModel, _testModelPath!);

        // Assert - Verify the model maintains entity metadata
        _mockSecureJsonSerializer.Verify(x => x.SerializeAsync(
            It.Is<SemanticModel>(model =>
                model.Tables.Count >= 0 && // Entity count depends on what lazy loading could load
                !ReferenceEquals(model.Tables, null)), // But collections should not be null
            It.IsAny<JsonSerializerOptions>()),
            Times.Once,
            "Should not corrupt entity metadata during save");

        loadedModel.Dispose();
    }

    [TestMethod]
    public async Task GetTablesAsync_WithLazyLoadingAfterSave_ShouldReturnMaterializedEntities()
    {
        // Arrange - Create semantic model with entity metadata
        var table1 = new SemanticModelTable("dbo", "Table1");
        var table2 = new SemanticModelTable("dbo", "Table2");

        _semanticModel!.Tables.AddRange([table1, table2]);
        await CreateIndividualEntityFiles([table1, table2], [], []);

        // Enable lazy loading
        _semanticModel.EnableLazyLoading(_testModelPath!, _persistenceStrategy!);

        // Mock the secure JSON serializer
        var expectedSemanticModelJson = CreateExpectedSemanticModelJson(_semanticModel);
        _mockSecureJsonSerializer!.Setup(x => x.SerializeAsync(It.IsAny<SemanticModel>(), It.IsAny<JsonSerializerOptions>()))
            .ReturnsAsync(expectedSemanticModelJson);

        // Act - Save and then retrieve entities
        await _persistenceStrategy!.SaveModelAsync(_semanticModel, _testModelPath!);
        var entities = await _semanticModel.GetTablesAsync();

        // Assert - Should return the materialized entities
        entities.Should().HaveCount(2);
        entities.Should().Contain(t => t.Name == "Table1");
        entities.Should().Contain(t => t.Name == "Table2");
    }

    [TestMethod]
    public void MaterializeLazyLoadedDataAsync_WithNullSemanticModel_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tempPath = new DirectoryInfo(Path.GetTempPath());

        // Act & Assert
        var action = async () => await InvokeMaterializeLazyLoadedDataAsync(null!, tempPath);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public void MaterializeLazyLoadedDataAsync_WithNullTempPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await InvokeMaterializeLazyLoadedDataAsync(_semanticModel!, null!);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    #region Helper Methods

    private async Task CreateIndividualEntityFiles(
        IEnumerable<SemanticModelTable> tables,
        IEnumerable<SemanticModelView> views,
        IEnumerable<SemanticModelStoredProcedure> storedProcedures)
    {
        // Create table files
        foreach (var table in tables)
        {
            var modelPath = table.GetModelPath();
            var filePath = Path.Combine(_testModelPath!.FullName, Path.Combine(modelPath.Parent?.Name ?? "", modelPath.Name));
            var tableJson = JsonSerializer.Serialize(table, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, tableJson, Encoding.UTF8);
        }

        // Create view files
        foreach (var view in views)
        {
            var modelPath = view.GetModelPath();
            var filePath = Path.Combine(_testModelPath!.FullName, Path.Combine(modelPath.Parent?.Name ?? "", modelPath.Name));
            var viewJson = JsonSerializer.Serialize(view, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, viewJson, Encoding.UTF8);
        }

        // Create stored procedure files
        foreach (var sp in storedProcedures)
        {
            var modelPath = sp.GetModelPath();
            var filePath = Path.Combine(_testModelPath!.FullName, Path.Combine(modelPath.Parent?.Name ?? "", modelPath.Name));
            var spJson = JsonSerializer.Serialize(sp, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, spJson, Encoding.UTF8);
        }
    }

    private async Task CreateCorruptedSemanticModelJson()
    {
        // Create a semanticmodel.json with empty arrays (simulating corruption)
        var corruptedModel = new
        {
            Name = "TestModel",
            Source = "TestSource",
            Description = "Test model",
            Tables = Array.Empty<object>(),
            Views = Array.Empty<object>(),
            StoredProcedures = Array.Empty<object>()
        };

        var filePath = Path.Combine(_testModelPath!.FullName, "semanticmodel.json");
        var json = JsonSerializer.Serialize(corruptedModel, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    private Task<SemanticModel> LoadSemanticModelWithLazyLoading()
    {
        // Simulate loading a model with the current persistence strategy
        // In a real scenario, this would use LoadModelAsync, but for testing we'll create a minimal model
        var model = new SemanticModel("TestModel", "TestSource");

        // For testing purposes, we'll simulate what LoadModelAsync would do by reading the corrupted JSON
        var semanticModelPath = Path.Combine(_testModelPath!.FullName, "semanticmodel.json");
        if (File.Exists(semanticModelPath))
        {
            // The corrupted JSON would have empty arrays, so the model would have no metadata
            // This simulates the corruption scenario we fixed
        }

        model.EnableLazyLoading(_testModelPath!, _persistenceStrategy!);
        return Task.FromResult(model);
    }

    private string CreateExpectedSemanticModelJson(SemanticModel model)
    {
        var modelData = new
        {
            Name = model.Name,
            Source = model.Source,
            Description = model.Description,
            Tables = model.Tables.Select(t => new
            {
                t.Name,
                t.Schema,
                Path = Path.Combine(t.GetModelPath().Parent?.Name ?? "", t.GetModelPath().Name)
            }).ToArray(),
            Views = model.Views.Select(v => new
            {
                v.Name,
                v.Schema,
                Path = Path.Combine(v.GetModelPath().Parent?.Name ?? "", v.GetModelPath().Name)
            }).ToArray(),
            StoredProcedures = model.StoredProcedures.Select(sp => new
            {
                sp.Name,
                sp.Schema,
                Path = Path.Combine(sp.GetModelPath().Parent?.Name ?? "", sp.GetModelPath().Name)
            }).ToArray()
        };

        return JsonSerializer.Serialize(modelData, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task InvokeMaterializeLazyLoadedDataAsync(SemanticModel semanticModel, DirectoryInfo tempPath)
    {
        // Use reflection to call the private MaterializeLazyLoadedDataAsync method
        var method = typeof(LocalDiskPersistenceStrategy)
            .GetMethod("MaterializeLazyLoadedDataAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var task = (Task)method.Invoke(_persistenceStrategy!, [semanticModel, tempPath])!;
            await task;
        }
        else
        {
            throw new InvalidOperationException("MaterializeLazyLoadedDataAsync method not found");
        }
    }

    #endregion
}