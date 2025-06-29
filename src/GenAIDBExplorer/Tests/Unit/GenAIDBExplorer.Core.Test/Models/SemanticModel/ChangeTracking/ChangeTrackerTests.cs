using System;
using System.Linq;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Models.SemanticModel.ChangeTracking;

/// <summary>
/// Unit tests for the ChangeTracker class.
/// </summary>
[TestClass]
public class ChangeTrackerTests
{
    private Mock<ILogger<ChangeTracker>>? _mockLogger;
    private ChangeTracker? _changeTracker;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<ChangeTracker>>();
        _changeTracker = new ChangeTracker(_mockLogger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _changeTracker?.Dispose();
    }

    [TestMethod]
    public void Constructor_WithLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        using var tracker = new ChangeTracker(_mockLogger!.Object);

        // Assert
        tracker.Should().NotBeNull();
        tracker.DirtyEntityCount.Should().Be(0);
        tracker.HasChanges.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithoutLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        using var tracker = new ChangeTracker();

        // Assert
        tracker.Should().NotBeNull();
        tracker.DirtyEntityCount.Should().Be(0);
        tracker.HasChanges.Should().BeFalse();
    }

    [TestMethod]
    public void MarkAsDirty_WithValidEntity_ShouldMarkEntityAsDirty()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        _changeTracker!.MarkAsDirty(entity);

        // Assert
        _changeTracker.IsDirty(entity).Should().BeTrue();
        _changeTracker.DirtyEntityCount.Should().Be(1);
        _changeTracker.HasChanges.Should().BeTrue();
    }

    [TestMethod]
    public void MarkAsDirty_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        _changeTracker!.Invoking(t => t.MarkAsDirty(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void MarkAsDirty_SameEntityMultipleTimes_ShouldOnlyTrackOnce()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        _changeTracker!.MarkAsDirty(entity);
        _changeTracker.MarkAsDirty(entity);
        _changeTracker.MarkAsDirty(entity);

        // Assert
        _changeTracker.IsDirty(entity).Should().BeTrue();
        _changeTracker.DirtyEntityCount.Should().Be(1);
        _changeTracker.GetDirtyEntities().Should().HaveCount(1);
    }

    [TestMethod]
    public void MarkAsClean_WithDirtyEntity_ShouldMarkEntityAsClean()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _changeTracker!.MarkAsDirty(entity);

        // Act
        _changeTracker.MarkAsClean(entity);

        // Assert
        _changeTracker.IsDirty(entity).Should().BeFalse();
        _changeTracker.DirtyEntityCount.Should().Be(0);
        _changeTracker.HasChanges.Should().BeFalse();
    }

    [TestMethod]
    public void MarkAsClean_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        _changeTracker!.Invoking(t => t.MarkAsClean(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void MarkAsClean_WithCleanEntity_ShouldRemainClean()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        _changeTracker!.MarkAsClean(entity);

        // Assert
        _changeTracker.IsDirty(entity).Should().BeFalse();
        _changeTracker.DirtyEntityCount.Should().Be(0);
    }

    [TestMethod]
    public void IsDirty_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        _changeTracker!.Invoking(t => t.IsDirty(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void IsDirty_WithUntrackedEntity_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act & Assert
        _changeTracker!.IsDirty(entity).Should().BeFalse();
    }

    [TestMethod]
    public void GetDirtyEntities_WithMultipleDirtyEntities_ShouldReturnAllDirtyEntities()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1, Name = "Test1" };
        var entity2 = new TestEntity { Id = 2, Name = "Test2" };
        var entity3 = new TestEntity { Id = 3, Name = "Test3" };

        _changeTracker!.MarkAsDirty(entity1);
        _changeTracker.MarkAsDirty(entity2);
        _changeTracker.MarkAsClean(entity3); // This should not be in dirty entities

        // Act
        var dirtyEntities = _changeTracker.GetDirtyEntities().ToList();

        // Assert
        dirtyEntities.Should().HaveCount(2);
        dirtyEntities.Should().Contain(entity1);
        dirtyEntities.Should().Contain(entity2);
        dirtyEntities.Should().NotContain(entity3);
    }

    [TestMethod]
    public void GetDirtyEntities_WithNoDirtyEntities_ShouldReturnEmpty()
    {
        // Arrange & Act
        var dirtyEntities = _changeTracker!.GetDirtyEntities();

        // Assert
        dirtyEntities.Should().BeEmpty();
    }

    [TestMethod]
    public void Clear_WithTrackedEntities_ShouldRemoveAllEntities()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1, Name = "Test1" };
        var entity2 = new TestEntity { Id = 2, Name = "Test2" };

        _changeTracker!.MarkAsDirty(entity1);
        _changeTracker.MarkAsDirty(entity2);

        // Act
        _changeTracker.Clear();

        // Assert
        _changeTracker.DirtyEntityCount.Should().Be(0);
        _changeTracker.HasChanges.Should().BeFalse();
        _changeTracker.GetDirtyEntities().Should().BeEmpty();
        _changeTracker.IsDirty(entity1).Should().BeFalse();
        _changeTracker.IsDirty(entity2).Should().BeFalse();
    }

    [TestMethod]
    public void AcceptAllChanges_WithDirtyEntities_ShouldMarkAllAsClean()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1, Name = "Test1" };
        var entity2 = new TestEntity { Id = 2, Name = "Test2" };

        _changeTracker!.MarkAsDirty(entity1);
        _changeTracker.MarkAsDirty(entity2);

        // Act
        _changeTracker.AcceptAllChanges();

        // Assert
        _changeTracker.DirtyEntityCount.Should().Be(0);
        _changeTracker.HasChanges.Should().BeFalse();
        _changeTracker.IsDirty(entity1).Should().BeFalse();
        _changeTracker.IsDirty(entity2).Should().BeFalse();
    }

    [TestMethod]
    public void EntityStateChanged_WhenMarkingAsDirty_ShouldRaiseEvent()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        EntityStateChangedEventArgs? capturedEventArgs = null;

        _changeTracker!.EntityStateChanged += (sender, e) => capturedEventArgs = e;

        // Act
        _changeTracker.MarkAsDirty(entity);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        capturedEventArgs!.Entity.Should().Be(entity);
        capturedEventArgs.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void EntityStateChanged_WhenMarkingAsClean_ShouldRaiseEvent()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _changeTracker!.MarkAsDirty(entity);

        EntityStateChangedEventArgs? capturedEventArgs = null;
        _changeTracker.EntityStateChanged += (sender, e) => capturedEventArgs = e;

        // Act
        _changeTracker.MarkAsClean(entity);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        capturedEventArgs!.Entity.Should().Be(entity);
        capturedEventArgs.IsDirty.Should().BeFalse();
    }

    [TestMethod]
    public void EntityStateChanged_WhenMarkingSameEntityDirtyTwice_ShouldOnlyRaiseEventOnce()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var eventCount = 0;

        _changeTracker!.EntityStateChanged += (sender, e) => eventCount++;

        // Act
        _changeTracker.MarkAsDirty(entity);
        _changeTracker.MarkAsDirty(entity);

        // Assert
        eventCount.Should().Be(1);
    }

    [TestMethod]
    public void Dispose_WithTrackedEntities_ShouldClearAllAndDispose()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _changeTracker!.MarkAsDirty(entity);

        // Act
        _changeTracker.Dispose();

        // Assert
        _changeTracker.Invoking(t => t.HasChanges)
            .Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Operations_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _changeTracker!.Dispose();

        // Act & Assert
        _changeTracker.Invoking(t => t.MarkAsDirty(entity))
            .Should().Throw<ObjectDisposedException>();

        _changeTracker.Invoking(t => t.MarkAsClean(entity))
            .Should().Throw<ObjectDisposedException>();

        _changeTracker.Invoking(t => t.IsDirty(entity))
            .Should().Throw<ObjectDisposedException>();

        _changeTracker.Invoking(t => t.GetDirtyEntities())
            .Should().Throw<ObjectDisposedException>();

        _changeTracker.Invoking(t => t.Clear())
            .Should().Throw<ObjectDisposedException>();

        _changeTracker.Invoking(t => t.AcceptAllChanges())
            .Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Test entity for change tracking tests.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
