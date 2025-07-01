using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel.LazyLoading;

/// <summary>
/// Unit tests for extended lazy loading functionality (Views and StoredProcedures) in Phase 4d.
/// </summary>
[TestClass]
public class ExtendedLazyLoadingTests
{
    private Mock<ISemanticModelPersistenceStrategy>? _mockStrategy;
    private DirectoryInfo? _testModelPath;
    private Core.Models.SemanticModel.SemanticModel? _semanticModel;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();

        // Create a temporary directory for testing
        var tempPath = Path.Combine(Path.GetTempPath(), "ExtendedLazyLoadingTests", Guid.NewGuid().ToString());
        _testModelPath = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(_testModelPath.FullName);

        // Create subdirectories for different entity types
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "tables"));
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "views"));
        Directory.CreateDirectory(Path.Combine(_testModelPath.FullName, "storedprocedures"));

        _semanticModel = new Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _semanticModel?.Dispose();
        
        if (_testModelPath?.Exists == true)
        {
            _testModelPath.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task GetViewsAsync_WithLazyLoadingDisabled_ShouldReturnEagerLoadedViews()
    {
        // Arrange
        var view1 = new Core.Models.SemanticModel.SemanticModelView("View1", "dbo", "Test view 1");
        var view2 = new Core.Models.SemanticModel.SemanticModelView("View2", "dbo", "Test view 2");
        _semanticModel!.Views.AddRange([view1, view2]);

        // Act
        var result = await _semanticModel.GetViewsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(view1);
        result.Should().Contain(view2);
        _semanticModel.IsLazyLoadingEnabled.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_WithLazyLoadingDisabled_ShouldReturnEagerLoadedStoredProcedures()
    {
        // Arrange
        var sp1 = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure1", "dbo", "Test procedure 1");
        var sp2 = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure2", "dbo", "Test procedure 2");
        _semanticModel!.StoredProcedures.AddRange([sp1, sp2]);

        // Act
        var result = await _semanticModel.GetStoredProceduresAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(sp1);
        result.Should().Contain(sp2);
        _semanticModel.IsLazyLoadingEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void EnableLazyLoading_ShouldSetIsLazyLoadingEnabledToTrue()
    {
        // Arrange & Act
        _semanticModel!.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        // Assert
        _semanticModel.IsLazyLoadingEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void EnableLazyLoading_ShouldClearEagerLoadedCollections()
    {
        // Arrange
        var table = new Core.Models.SemanticModel.SemanticModelTable("Table1", "dbo", "Test table");
        var view = new Core.Models.SemanticModel.SemanticModelView("View1", "dbo", "Test view");
        var storedProcedure = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure1", "dbo", "Test procedure");

        _semanticModel!.Tables.Add(table);
        _semanticModel.Views.Add(view);
        _semanticModel.StoredProcedures.Add(storedProcedure);

        // Act
        _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        // Assert
        _semanticModel.Tables.Should().BeEmpty();
        _semanticModel.Views.Should().BeEmpty();
        _semanticModel.StoredProcedures.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetViewsAsync_WithLazyLoadingEnabled_ShouldReturnLazyLoadedViews()
    {
        // Arrange
        var view1 = new Core.Models.SemanticModel.SemanticModelView("View1", "dbo", "Test view 1");
        var view2 = new Core.Models.SemanticModel.SemanticModelView("View2", "dbo", "Test view 2");
        _semanticModel!.Views.AddRange([view1, view2]);

        _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        // Act
        var result = await _semanticModel.GetViewsAsync();

        // Assert
        result.Should().HaveCount(2);
        _semanticModel.IsLazyLoadingEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_WithLazyLoadingEnabled_ShouldReturnLazyLoadedStoredProcedures()
    {
        // Arrange
        var sp1 = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure1", "dbo", "Test procedure 1");
        var sp2 = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure2", "dbo", "Test procedure 2");
        _semanticModel!.StoredProcedures.AddRange([sp1, sp2]);

        _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        // Act
        var result = await _semanticModel.GetStoredProceduresAsync();

        // Assert
        result.Should().HaveCount(2);
        _semanticModel.IsLazyLoadingEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetViewsAsync_WithNonExistentViewsDirectory_ShouldReturnEmptyCollection()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "NoViewsDirectory", Guid.NewGuid().ToString());
        var testPath = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(testPath.FullName);

        try
        {
            var view = new Core.Models.SemanticModel.SemanticModelView("View1", "dbo", "Test view");
            _semanticModel!.Views.Add(view);
            _semanticModel.EnableLazyLoading(testPath, _mockStrategy!.Object);

            // Act
            var result = await _semanticModel.GetViewsAsync();

            // Assert
            result.Should().BeEmpty();
        }
        finally
        {
            if (testPath.Exists)
            {
                testPath.Delete(recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_WithNonExistentStoredProceduresDirectory_ShouldReturnEmptyCollection()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "NoStoredProceduresDirectory", Guid.NewGuid().ToString());
        var testPath = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(testPath.FullName);

        try
        {
            var sp = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure1", "dbo", "Test procedure");
            _semanticModel!.StoredProcedures.Add(sp);
            _semanticModel.EnableLazyLoading(testPath, _mockStrategy!.Object);

            // Act
            var result = await _semanticModel.GetStoredProceduresAsync();

            // Assert
            result.Should().BeEmpty();
        }
        finally
        {
            if (testPath.Exists)
            {
                testPath.Delete(recursive: true);
            }
        }
    }

    [TestMethod]
    public void EnableLazyLoading_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _semanticModel!.Dispose();

        // Act & Assert
        var action = () => _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);
        action.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task GetViewsAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _semanticModel!.Dispose();

        // Act & Assert
        var action = async () => await _semanticModel.GetViewsAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _semanticModel!.Dispose();

        // Act & Assert
        var action = async () => await _semanticModel.GetStoredProceduresAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task BackwardCompatibility_ExistingTablesLazyLoading_ShouldContinueToWork()
    {
        // Arrange
        var table1 = new Core.Models.SemanticModel.SemanticModelTable("Table1", "dbo", "Test table 1");
        var table2 = new Core.Models.SemanticModel.SemanticModelTable("Table2", "dbo", "Test table 2");
        _semanticModel!.Tables.AddRange([table1, table2]);

        _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);

        // Act
        var result = await _semanticModel.GetTablesAsync();

        // Assert
        result.Should().HaveCount(2);
        _semanticModel.IsLazyLoadingEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void Dispose_WithLazyLoadingEnabled_ShouldCleanUpAllProxies()
    {
        // Arrange
        var table = new Core.Models.SemanticModel.SemanticModelTable("Table1", "dbo", "Test table");
        var view = new Core.Models.SemanticModel.SemanticModelView("View1", "dbo", "Test view");
        var sp = new Core.Models.SemanticModel.SemanticModelStoredProcedure("Procedure1", "dbo", "Test procedure");

        _semanticModel!.Tables.Add(table);
        _semanticModel.Views.Add(view);
        _semanticModel.StoredProcedures.Add(sp);

        _semanticModel.EnableLazyLoading(_testModelPath!, _mockStrategy!.Object);
        _semanticModel.IsLazyLoadingEnabled.Should().BeTrue();

        // Act
        _semanticModel.Dispose();

        // Assert
        var action1 = async () => await _semanticModel.GetTablesAsync();
        var action2 = async () => await _semanticModel.GetViewsAsync();
        var action3 = async () => await _semanticModel.GetStoredProceduresAsync();

        action1.Should().ThrowAsync<ObjectDisposedException>();
        action2.Should().ThrowAsync<ObjectDisposedException>();
        action3.Should().ThrowAsync<ObjectDisposedException>();
    }
}
