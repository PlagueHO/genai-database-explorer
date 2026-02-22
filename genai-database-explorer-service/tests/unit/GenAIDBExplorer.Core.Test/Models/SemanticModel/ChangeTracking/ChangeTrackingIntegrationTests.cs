using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using SemanticModelClass = GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel;

namespace GenAIDBExplorer.Core.Test.Models.SemanticModel.ChangeTracking;

/// <summary>
/// Simple integration tests for change tracking functionality.
/// </summary>
[TestClass]
public class ChangeTrackingIntegrationTests
{
    [TestMethod]
    public void SemanticModel_WithChangeTracking_ShouldTrackEntityChanges()
    {
        // Arrange
        var model = new SemanticModelClass("TestModel", "TestSource");
        var changeTracker = new ChangeTracker();
        model.EnableChangeTracking(changeTracker);

        // Act
        var table = new SemanticModelTable("TestSchema", "TestTable");
        model.AddTable(table);

        // Assert
        model.IsChangeTrackingEnabled.Should().BeTrue();
        model.HasUnsavedChanges.Should().BeTrue();
        model.ChangeTracker.Should().NotBeNull();
        model.ChangeTracker!.IsDirty(table).Should().BeTrue();

        // Clean up
        model.Dispose();
        changeTracker.Dispose();
    }

    [TestMethod]
    public void ChangeTracker_BasicOperations_ShouldWork()
    {
        // Arrange
        using var changeTracker = new ChangeTracker();
        var testEntity = new { Id = 1, Name = "Test" };

        // Act & Assert
        changeTracker.HasChanges.Should().BeFalse();

        changeTracker.MarkAsDirty(testEntity);
        changeTracker.HasChanges.Should().BeTrue();
        changeTracker.IsDirty(testEntity).Should().BeTrue();

        changeTracker.MarkAsClean(testEntity);
        changeTracker.HasChanges.Should().BeFalse();
        changeTracker.IsDirty(testEntity).Should().BeFalse();
    }
}
