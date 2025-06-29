using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository;

/// <summary>
/// Unit tests for SemanticModelRepository change tracking functionality.
/// </summary>
[TestClass]
public class SemanticModelRepositoryChangeTrackingTests
{
    private Mock<IPersistenceStrategyFactory>? _mockStrategyFactory;
    private Mock<ISemanticModelPersistenceStrategy>? _mockStrategy;
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
            _mockLoggerFactory.Object);

        _testModelPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
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
    public async Task LoadModelAsync_WithChangeTrackingEnabled_ShouldEnableChangeTracking()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: false, enableChangeTracking: true);

        // Assert
        result.Should().NotBeNull();
        result.IsChangeTrackingEnabled.Should().BeTrue();
        result.ChangeTracker.Should().NotBeNull();
        result.HasUnsavedChanges.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadModelAsync_WithChangeTrackingDisabled_ShouldNotEnableChangeTracking()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: false, enableChangeTracking: false);

        // Assert
        result.Should().NotBeNull();
        result.IsChangeTrackingEnabled.Should().BeFalse();
        result.ChangeTracker.Should().BeNull();
        result.HasUnsavedChanges.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadModelAsync_WithBothLazyLoadingAndChangeTracking_ShouldEnableBoth()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: true, enableChangeTracking: true);

        // Assert
        result.Should().NotBeNull();
        result.IsLazyLoadingEnabled.Should().BeTrue();
        result.IsChangeTrackingEnabled.Should().BeTrue();
        result.ChangeTracker.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithoutChangeTracking_ShouldPerformFullSave()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");

        // Act
        await _repository!.SaveChangesAsync(model, _testModelPath!);

        // Assert
        _mockStrategy!.Verify(s => s.SaveModelAsync(model, It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithChangeTrackingButNoChanges_ShouldPerformFullSave()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        model.EnableChangeTracking(changeTracker);

        // Act
        await _repository!.SaveChangesAsync(model, _testModelPath!);

        // Assert
        _mockStrategy!.Verify(s => s.SaveModelAsync(model, It.IsAny<DirectoryInfo>()), Times.Once);
        model.HasUnsavedChanges.Should().BeFalse();
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithChangeTrackingAndChanges_ShouldPerformSaveAndAcceptChanges()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        model.EnableChangeTracking(changeTracker);

        // Add a table to make the model dirty
        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);

        model.HasUnsavedChanges.Should().BeTrue(); // Verify model is dirty

        // Act
        await _repository!.SaveChangesAsync(model, _testModelPath!);

        // Assert
        _mockStrategy!.Verify(s => s.SaveModelAsync(model, It.IsAny<DirectoryInfo>()), Times.Once);
        model.HasUnsavedChanges.Should().BeFalse(); // Changes should be accepted
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithStrategy_ShouldUseSpecifiedStrategy()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        model.EnableChangeTracking(changeTracker);

        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);

        const string strategyName = "TestStrategy";

        // Act
        await _repository!.SaveChangesAsync(model, _testModelPath!, strategyName);

        // Assert
        _mockStrategyFactory!.Verify(f => f.GetStrategy(strategyName), Times.Once);
        _mockStrategy!.Verify(s => s.SaveModelAsync(model, It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task SaveModelAsync_ShouldAlwaysPerformFullSave()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        model.EnableChangeTracking(changeTracker);

        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);

        // Act
        await _repository!.SaveModelAsync(model, _testModelPath!);

        // Assert
        _mockStrategy!.Verify(s => s.SaveModelAsync(model, It.IsAny<DirectoryInfo>()), Times.Once);
        // Note: SaveModelAsync should NOT accept changes automatically - that's only for SaveChangesAsync
        model.HasUnsavedChanges.Should().BeTrue();
    }

    [TestMethod]
    public async Task ChangeTracking_EntityModifications_ShouldTrackChanges()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var model = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: false, enableChangeTracking: true);

        // Act & Assert - Adding entities should mark them as dirty
        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);
        model.HasUnsavedChanges.Should().BeTrue();
        model.ChangeTracker!.IsDirty(testTable).Should().BeTrue();

        var testView = new SemanticModelView("TestSchema", "TestView");
        model.AddView(testView);
        model.ChangeTracker!.IsDirty(testView).Should().BeTrue();

        var testStoredProcedure = new SemanticModelStoredProcedure("TestSchema", "TestProcedure", "Test Stored Procedure");
        model.AddStoredProcedure(testStoredProcedure);
        model.ChangeTracker!.IsDirty(testStoredProcedure).Should().BeTrue();

        // Removing entities should also mark them as dirty
        model.RemoveTable(testTable);
        model.ChangeTracker!.IsDirty(testTable).Should().BeTrue();

        model.RemoveView(testView);
        model.ChangeTracker!.IsDirty(testView).Should().BeTrue();

        model.RemoveStoredProcedure(testStoredProcedure);
        model.ChangeTracker!.IsDirty(testStoredProcedure).Should().BeTrue();
    }

    [TestMethod]
    public async Task AcceptAllChanges_ShouldMarkAllEntitiesAsClean()
    {
        // Arrange
        var expectedModel = new SemanticModel("TestModel", "TestSource", "Test Description");
        _mockStrategy!.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(expectedModel);

        var model = await _repository!.LoadModelAsync(_testModelPath!, enableLazyLoading: false, enableChangeTracking: true);

        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);
        model.HasUnsavedChanges.Should().BeTrue();

        // Act
        model.AcceptAllChanges();

        // Assert
        model.HasUnsavedChanges.Should().BeFalse();
        model.ChangeTracker!.IsDirty(testTable).Should().BeFalse();
    }

    [TestMethod]
    public void ModelDispose_WithChangeTracking_ShouldDisposeChangeTracker()
    {
        // Arrange
        var model = new SemanticModel("TestModel", "TestSource", "Test Description");
        var changeTracker = new ChangeTracker(_mockChangeTrackerLogger!.Object);
        model.EnableChangeTracking(changeTracker);

        var testTable = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(testTable);

        // Act
        model.Dispose();

        // Assert
        // After disposal, the change tracker should also be disposed
        // We can't directly test this, but we can verify that the model no longer has change tracking enabled
        model.Invoking(m => m.IsChangeTrackingEnabled)
            .Should().Throw<ObjectDisposedException>();
    }
}
