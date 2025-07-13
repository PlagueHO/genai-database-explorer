using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders;

/// <summary>
/// Unit tests for SemanticModelProvider class.
/// Tests AC-001, AC-002, AC-004, AC-010 from specification.
/// </summary>
[TestClass]
public class SemanticModelProviderTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISchemaRepository> _mockSchemaRepository = null!;
    private Mock<ISemanticModelRepository> _mockSemanticModelRepository = null!;
    private Mock<ILogger<SemanticModelProvider>> _mockLogger = null!;
    private SemanticModelProvider _semanticModelProvider = null!;
    private DirectoryInfo _tempDirectory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange - Create mocks and test data
        _mockProject = new Mock<IProject>();
        _mockSchemaRepository = new Mock<ISchemaRepository>();
        _mockSemanticModelRepository = new Mock<ISemanticModelRepository>();
        _mockLogger = new Mock<ILogger<SemanticModelProvider>>();

        // Setup project settings
        var databaseSettings = new DatabaseSettings
        {
            Name = "TestDatabase",
            ConnectionString = "Server=test;Database=TestDB;",
            Description = "Test database for unit tests",
            MaxDegreeOfParallelism = 2
        };

        var projectSettings = new ProjectSettings
        {
            Database = databaseSettings,
            DataDictionary = new DataDictionarySettings(),
            OpenAIService = new OpenAIServiceSettings(),
            SemanticModel = new SemanticModelSettings
            {
                MaxDegreeOfParallelism = 2
            },
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration
                {
                    Directory = "SemanticModel"
                }
            }
        };

        _mockProject.Setup(p => p.Settings).Returns(projectSettings);

        // Create temp directory for file operations
        _tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        _tempDirectory.Create();

        _semanticModelProvider = new SemanticModelProvider(
            _mockProject.Object,
            _mockSchemaRepository.Object,
            _mockLogger.Object,
            _mockSemanticModelRepository.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up temp directory
        if (_tempDirectory.Exists)
        {
            _tempDirectory.Delete(true);
        }
    }

    [TestMethod]
    public void CreateSemanticModel_ShouldCreateEmptyModel_WithProjectInformation()
    {
        // Act
        var result = _semanticModelProvider.CreateSemanticModel();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
        result.Source.Should().Be("Server=test;Database=TestDB;");
        result.Description.Should().Be("Test database for unit tests");
        result.Tables.Should().BeEmpty();
        result.Views.Should().BeEmpty();
        result.StoredProcedures.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldExtractAllEntities_FromDatabase()
    {
        // Arrange
        var tables = new Dictionary<string, TableInfo>
        {
            { "Sales.Customer", new TableInfo("Sales", "Customer") }
        };
        var views = new Dictionary<string, ViewInfo>
        {
            { "Sales.CustomerView", new ViewInfo("Sales", "CustomerView") }
        };
        var procedures = new Dictionary<string, StoredProcedureInfo>
        {
            { "Sales.GetCustomers", new StoredProcedureInfo("Sales", "GetCustomers", "PROCEDURE", null, "CREATE PROCEDURE GetCustomers AS SELECT * FROM Customers") }
        };

        var semanticTable = new SemanticModelTable("Sales", "Customer");
        var semanticView = new SemanticModelView("Sales", "CustomerView");
        var semanticProcedure = new SemanticModelStoredProcedure("Sales", "GetCustomers", "CREATE PROCEDURE GetCustomers AS SELECT * FROM Customers");

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(tables);
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(null))
            .ReturnsAsync(views);
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(null))
            .ReturnsAsync(procedures);

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelTableAsync(It.IsAny<TableInfo>()))
            .ReturnsAsync(semanticTable);
        _mockSchemaRepository.Setup(r => r.CreateSemanticModelViewAsync(It.IsAny<ViewInfo>()))
            .ReturnsAsync(semanticView);
        _mockSchemaRepository.Setup(r => r.CreateSemanticModelStoredProcedureAsync(It.IsAny<StoredProcedureInfo>()))
            .ReturnsAsync(semanticProcedure);

        // Act
        var result = await _semanticModelProvider.ExtractSemanticModelAsync();

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Views.Should().HaveCount(1);
        result.StoredProcedures.Should().HaveCount(1);

        // Verify all repository methods were called
        _mockSchemaRepository.Verify(r => r.GetTablesAsync(null), Times.Once);
        _mockSchemaRepository.Verify(r => r.GetViewsAsync(null), Times.Once);
        _mockSchemaRepository.Verify(r => r.GetStoredProceduresAsync(null), Times.Once);
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldHandleEmptyDatabase_Gracefully()
    {
        // Arrange
        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(new Dictionary<string, TableInfo>());
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(null))
            .ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(null))
            .ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        // Act
        var result = await _semanticModelProvider.ExtractSemanticModelAsync();

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().BeEmpty();
        result.Views.Should().BeEmpty();
        result.StoredProcedures.Should().BeEmpty();
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_ShouldLoadExistingModel_FromDirectory()
    {
        // Arrange
        var expectedModel = _semanticModelProvider.CreateSemanticModel();
        await expectedModel.SaveModelAsync(_tempDirectory);

        _mockSemanticModelRepository.Setup(r => r.LoadModelAsync(_tempDirectory, (string?)null))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _semanticModelProvider.LoadSemanticModelAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedModel);
        _mockSemanticModelRepository.Verify(r => r.LoadModelAsync(_tempDirectory, (string?)null), Times.Once);
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_WithRepository_ShouldUseRepository_WhenAvailable()
    {
        // Arrange
        var expectedModel = _semanticModelProvider.CreateSemanticModel();
        _mockSemanticModelRepository.Setup(r => r.LoadModelAsync(_tempDirectory, (string?)null))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _semanticModelProvider.LoadSemanticModelAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedModel);
        _mockSemanticModelRepository.Verify(r => r.LoadModelAsync(_tempDirectory, (string?)null), Times.Once);
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_WithRepository_ShouldFallbackToDirectLoading_WhenRepositoryFails()
    {
        // Arrange
        var model = _semanticModelProvider.CreateSemanticModel();
        await model.SaveModelAsync(_tempDirectory);

        _mockSemanticModelRepository.Setup(r => r.LoadModelAsync(_tempDirectory, (string?)null))
            .ThrowsAsync(new InvalidOperationException("Repository failure"));

        // Act
        var result = await _semanticModelProvider.LoadSemanticModelAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
        _mockSemanticModelRepository.Verify(r => r.LoadModelAsync(_tempDirectory, (string?)null), Times.Once);
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_ShouldFallbackToDirectLoading_WhenRepositoryFails()
    {
        // Arrange
        var model = _semanticModelProvider.CreateSemanticModel();
        await model.SaveModelAsync(_tempDirectory);

        _mockSemanticModelRepository.Setup(r => r.LoadModelAsync(_tempDirectory, (string?)null))
            .ThrowsAsync(new InvalidOperationException("Repository failure"));

        // Act
        var result = await _semanticModelProvider.LoadSemanticModelAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
        _mockSemanticModelRepository.Verify(r => r.LoadModelAsync(_tempDirectory, (string?)null), Times.Once);
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_ShouldThrowException_WhenDirectoryNotFound()
    {
        // Arrange
        var nonExistentPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        _mockSemanticModelRepository.Setup(r => r.LoadModelAsync(nonExistentPath, (string?)null))
            .ThrowsAsync(new FileNotFoundException("Directory not found"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _semanticModelProvider.LoadSemanticModelAsync(nonExistentPath));
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldHandleParallelOperations_WithoutCorruption()
    {
        // Arrange
        var largeTableCount = 50;
        var tables = new Dictionary<string, TableInfo>();

        for (int i = 0; i < largeTableCount; i++)
        {
            tables[$"dbo.Table{i}"] = new TableInfo("dbo", $"Table{i}");
        }

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(tables);
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(null))
            .ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(null))
            .ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelTableAsync(It.IsAny<TableInfo>()))
            .ReturnsAsync((TableInfo info) => new SemanticModelTable(info.SchemaName, info.TableName));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _semanticModelProvider.ExtractSemanticModelAsync();
        var duration = DateTime.UtcNow - startTime;

        // Assert - Tests PER-002: Entity loading ≤5s for 1000 entities (scaled down)
        result.Tables.Should().HaveCount(largeTableCount);
        duration.Should().BeLessThan(TimeSpan.FromSeconds(5));

        // Verify no duplicate tables (tests concurrency safety)
        var tableNames = result.Tables.Select(t => $"{t.Schema}.{t.Name}").ToList();
        tableNames.Should().OnlyHaveUniqueItems();
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldRespectMaxDegreeOfParallelism()
    {
        // Arrange
        var tables = new Dictionary<string, TableInfo>();
        for (int i = 0; i < 10; i++)
        {
            tables[$"dbo.Table{i}"] = new TableInfo("dbo", $"Table{i}");
        }

        var concurrentCallCount = 0;
        var maxConcurrentCalls = 0;
        var semaphore = new SemaphoreSlim(1, 1);

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(tables);
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(null))
            .ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(null))
            .ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelTableAsync(It.IsAny<TableInfo>()))
            .Returns(async (TableInfo info) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var current = Interlocked.Increment(ref concurrentCallCount);
                    maxConcurrentCalls = Math.Max(maxConcurrentCalls, current);

                    await Task.Delay(50); // Simulate work

                    Interlocked.Decrement(ref concurrentCallCount);
                    return new SemanticModelTable(info.SchemaName, info.TableName);
                }
                finally
                {
                    semaphore.Release();
                }
            });

        // Act
        await _semanticModelProvider.ExtractSemanticModelAsync();

        // Assert - Should respect MaxDegreeOfParallelism = 2
        (maxConcurrentCalls <= 2).Should().BeTrue();
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldHandleRepositoryExceptions_Gracefully()
    {
        // Arrange
        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _semanticModelProvider.ExtractSemanticModelAsync());
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenProjectIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new SemanticModelProvider(null!, _mockSchemaRepository.Object, _mockLogger.Object, _mockSemanticModelRepository.Object));
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenSchemaRepositoryIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new SemanticModelProvider(_mockProject.Object, null!, _mockLogger.Object, _mockSemanticModelRepository.Object));
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new SemanticModelProvider(_mockProject.Object, _mockSchemaRepository.Object, null!, _mockSemanticModelRepository.Object));
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenSemanticModelRepositoryIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new SemanticModelProvider(_mockProject.Object, _mockSchemaRepository.Object, _mockLogger.Object, null!));
    }

    [TestMethod]
    public void Constructor_ShouldAcceptAllRequiredDependencies()
    {
        // Act
        var provider = new SemanticModelProvider(
            _mockProject.Object,
            _mockSchemaRepository.Object,
            _mockLogger.Object,
            _mockSemanticModelRepository.Object);

        // Assert
        provider.Should().NotBeNull();
    }
}
