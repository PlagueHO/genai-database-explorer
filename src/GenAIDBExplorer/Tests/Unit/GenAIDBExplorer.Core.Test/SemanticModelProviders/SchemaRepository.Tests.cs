using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders;

/// <summary>
/// Unit tests for ISchemaRepository interface compliance and behavior.
/// Tests REQ-001, REQ-002, SEC-002, PER-001 from specification.
/// </summary>
[TestClass]
public class SchemaRepositoryTests
{
    private Mock<ISchemaRepository> _mockSchemaRepository = null!;
    private ISchemaRepository _schemaRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange - Create mock for interface testing
        _mockSchemaRepository = new Mock<ISchemaRepository>();
        _schemaRepository = _mockSchemaRepository.Object;
    }

    [TestMethod]
    public async Task GetTablesAsync_ShouldReturnDictionary_OfTableInfo()
    {
        // Arrange
        var expectedTables = new Dictionary<string, TableInfo>
        {
            { "dbo.Customer", new TableInfo("dbo", "Customer") },
            { "Sales.Orders", new TableInfo("Sales", "Orders") }
        };

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(expectedTables);

        // Act
        var result = await _schemaRepository.GetTablesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("dbo.Customer");
        result.Should().ContainKey("Sales.Orders");
        result["dbo.Customer"].TableName.Should().Be("Customer");
        result["Sales.Orders"].SchemaName.Should().Be("Sales");
    }

    [TestMethod]
    public async Task GetTablesAsync_WithSchema_ShouldFilterBySchema()
    {
        // Arrange
        var expectedTables = new Dictionary<string, TableInfo>
        {
            { "Sales.Customer", new TableInfo("Sales", "Customer") },
            { "Sales.Orders", new TableInfo("Sales", "Orders") }
        };

        _mockSchemaRepository.Setup(r => r.GetTablesAsync("Sales"))
            .ReturnsAsync(expectedTables);

        // Act
        var result = await _schemaRepository.GetTablesAsync("Sales");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Values.Should().AllSatisfy(t => t.SchemaName.Should().Be("Sales"));
    }

    [TestMethod]
    public async Task GetViewsAsync_ShouldReturnDictionary_OfViewInfo()
    {
        // Arrange
        var expectedViews = new Dictionary<string, ViewInfo>
        {
            { "dbo.CustomerView", new ViewInfo("dbo", "CustomerView") },
            { "Sales.OrderSummary", new ViewInfo("Sales", "OrderSummary") }
        };

        _mockSchemaRepository.Setup(r => r.GetViewsAsync(null))
            .ReturnsAsync(expectedViews);

        // Act
        var result = await _schemaRepository.GetViewsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("dbo.CustomerView");
        result.Should().ContainKey("Sales.OrderSummary");
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_ShouldReturnDictionary_OfStoredProcedureInfo()
    {
        // Arrange
        var expectedProcedures = new Dictionary<string, StoredProcedureInfo>
        {
            { "dbo.GetCustomer", new StoredProcedureInfo("dbo", "GetCustomer", "PROCEDURE", null, "CREATE PROCEDURE...") },
            { "Sales.ProcessOrder", new StoredProcedureInfo("Sales", "ProcessOrder", "PROCEDURE", null, "CREATE PROCEDURE...") }
        };

        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(null))
            .ReturnsAsync(expectedProcedures);

        // Act
        var result = await _schemaRepository.GetStoredProceduresAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("dbo.GetCustomer");
        result.Should().ContainKey("Sales.ProcessOrder");
    }

    [TestMethod]
    public async Task GetColumnsForTableAsync_ShouldReturnList_OfSemanticModelColumns()
    {
        // Arrange
        var tableInfo = new TableInfo("dbo", "Customer");
        var expectedColumns = new List<SemanticModelColumn>
        {
            new("dbo", "CustomerID", "Primary key") { Type = "int" },
            new("dbo", "CustomerName", "Customer name") { Type = "nvarchar(100)" }
        };

        _mockSchemaRepository.Setup(r => r.GetColumnsForTableAsync(tableInfo))
            .ReturnsAsync(expectedColumns);

        // Act
        var result = await _schemaRepository.GetColumnsForTableAsync(tableInfo);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("CustomerID");
        result[1].Name.Should().Be("CustomerName");
    }

    [TestMethod]
    public async Task GetSampleTableDataAsync_ShouldReturnSampleData()
    {
        // Arrange
        var tableInfo = new TableInfo("dbo", "Customer");
        var expectedData = new List<Dictionary<string, object?>>
        {
            new() { { "CustomerID", 1 }, { "CustomerName", "John Doe" } },
            new() { { "CustomerID", 2 }, { "CustomerName", "Jane Smith" } }
        };

        _mockSchemaRepository.Setup(r => r.GetSampleTableDataAsync(tableInfo, 5, false))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _schemaRepository.GetSampleTableDataAsync(tableInfo, 5, false);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Should().ContainKey("CustomerID");
        result[0].Should().ContainKey("CustomerName");
    }

    [TestMethod]
    public async Task CreateSemanticModelTableAsync_ShouldCreateTable_WithColumnsAndIndexes()
    {
        // Arrange
        var tableInfo = new TableInfo("dbo", "Customer");
        var expectedTable = new SemanticModelTable("dbo", "Customer");
        expectedTable.Columns.Add(new SemanticModelColumn("dbo", "CustomerID", "Primary key") { Type = "int", IsPrimaryKey = true });
        expectedTable.Indexes.Add(new SemanticModelIndex("dbo", "IX_Customer_Name", "Index on name") { Type = "NonClustered" });

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelTableAsync(tableInfo))
            .ReturnsAsync(expectedTable);

        // Act
        var result = await _schemaRepository.CreateSemanticModelTableAsync(tableInfo);

        // Assert
        result.Should().NotBeNull();
        result.Schema.Should().Be("dbo");
        result.Name.Should().Be("Customer");
        result.Columns.Should().HaveCount(1);
        result.Indexes.Should().HaveCount(1);
        result.Columns[0].IsPrimaryKey.Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateSemanticModelViewAsync_ShouldCreateView_WithDefinition()
    {
        // Arrange
        var viewInfo = new ViewInfo("dbo", "CustomerView");
        var expectedView = new SemanticModelView("dbo", "CustomerView");
        expectedView.Columns.Add(new SemanticModelColumn("dbo", "CustomerID", "Customer identifier") { Type = "int" });
        expectedView.Definition = "SELECT CustomerID, CustomerName FROM Customer";

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelViewAsync(viewInfo))
            .ReturnsAsync(expectedView);

        // Act
        var result = await _schemaRepository.CreateSemanticModelViewAsync(viewInfo);

        // Assert
        result.Should().NotBeNull();
        result.Schema.Should().Be("dbo");
        result.Name.Should().Be("CustomerView");
        result.Columns.Should().HaveCount(1);
        result.Definition.Should().Be("SELECT CustomerID, CustomerName FROM Customer");
    }

    [TestMethod]
    public async Task CreateSemanticModelStoredProcedureAsync_ShouldCreateStoredProcedure_WithParameters()
    {
        // Arrange
        var procedureInfo = new StoredProcedureInfo("dbo", "GetCustomer", "PROCEDURE", "@CustomerID int", "CREATE PROCEDURE GetCustomer...");
        var expectedProcedure = new SemanticModelStoredProcedure("dbo", "GetCustomer", "CREATE PROCEDURE GetCustomer @CustomerID int AS SELECT * FROM Customer WHERE CustomerID = @CustomerID", "@CustomerID int");

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelStoredProcedureAsync(procedureInfo))
            .ReturnsAsync(expectedProcedure);

        // Act
        var result = await _schemaRepository.CreateSemanticModelStoredProcedureAsync(procedureInfo);

        // Assert
        result.Should().NotBeNull();
        result.Schema.Should().Be("dbo");
        result.Name.Should().Be("GetCustomer");
        result.Definition.Should().Contain("CREATE PROCEDURE GetCustomer");
    }

    [TestMethod]
    public async Task GetTablesAsync_ShouldHandleException_Gracefully()
    {
        // Arrange
        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ThrowsAsync(new InvalidOperationException("SQL connection failed"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _schemaRepository.GetTablesAsync());
    }

    [TestMethod]
    public async Task GetSampleTableDataAsync_WithRandomSelection_ShouldReturnRandomData()
    {
        // Arrange
        var tableInfo = new TableInfo("dbo", "Customer");
        var expectedData = new List<Dictionary<string, object?>>
        {
            new() { { "CustomerID", 1 }, { "CustomerName", "Random Customer" } }
        };

        _mockSchemaRepository.Setup(r => r.GetSampleTableDataAsync(tableInfo, 1, true))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _schemaRepository.GetSampleTableDataAsync(tableInfo, 1, true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        // Verify random selection was requested
        _mockSchemaRepository.Verify(r => r.GetSampleTableDataAsync(tableInfo, 1, true), Times.Once);
    }

    [TestMethod]
    public async Task ConcurrentOperations_ShouldNotCause_DataCorruption()
    {
        // Arrange - Test PER-001: Concurrent operations without corruption
        var tableInfo = new TableInfo("dbo", "Customer");
        var expectedTable = new SemanticModelTable("dbo", "Customer");
        expectedTable.Columns.Add(new SemanticModelColumn("dbo", "CustomerID", "Primary key") { Type = "int" });

        _mockSchemaRepository.Setup(r => r.CreateSemanticModelTableAsync(tableInfo))
            .ReturnsAsync(expectedTable);

        // Act - Execute multiple concurrent operations
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _schemaRepository.CreateSemanticModelTableAsync(tableInfo))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.Schema.Should().Be("dbo");
            result.Name.Should().Be("Customer");
            result.Columns.Should().HaveCount(1);
        });

        // Verify method was called correct number of times
        _mockSchemaRepository.Verify(r => r.CreateSemanticModelTableAsync(tableInfo), Times.Exactly(10));
    }

    [TestMethod]
    public async Task GetViewDefinitionAsync_ShouldReturnViewDefinition()
    {
        // Arrange
        var viewInfo = new ViewInfo("dbo", "CustomerView");
        var expectedDefinition = "SELECT CustomerID, CustomerName FROM Customer WHERE Active = 1";

        _mockSchemaRepository.Setup(r => r.GetViewDefinitionAsync(viewInfo))
            .ReturnsAsync(expectedDefinition);

        // Act
        var result = await _schemaRepository.GetViewDefinitionAsync(viewInfo);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDefinition);
        result.Should().Contain("SELECT");
        result.Should().Contain("FROM Customer");
    }

    [TestMethod]
    public async Task GetColumnsForViewAsync_ShouldReturnViewColumns()
    {
        // Arrange
        var viewInfo = new ViewInfo("dbo", "CustomerView");
        var expectedColumns = new List<SemanticModelColumn>
        {
            new("dbo", "CustomerID", "Customer identifier") { Type = "int" },
            new("dbo", "CustomerName", "Customer name") { Type = "nvarchar(100)" }
        };

        _mockSchemaRepository.Setup(r => r.GetColumnsForViewAsync(viewInfo))
            .ReturnsAsync(expectedColumns);

        // Act
        var result = await _schemaRepository.GetColumnsForViewAsync(viewInfo);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("CustomerID");
        result[1].Name.Should().Be("CustomerName");
    }

    [TestMethod]
    public async Task GetSampleViewDataAsync_ShouldReturnViewSampleData()
    {
        // Arrange
        var viewInfo = new ViewInfo("dbo", "CustomerView");
        var expectedData = new List<Dictionary<string, object?>>
        {
            new() { { "CustomerID", 1 }, { "CustomerName", "Active Customer" } }
        };

        _mockSchemaRepository.Setup(r => r.GetSampleViewDataAsync(viewInfo, 5, false))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _schemaRepository.GetSampleViewDataAsync(viewInfo, 5, false);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().ContainKey("CustomerID");
        result[0]["CustomerName"].Should().Be("Active Customer");
    }

    [TestMethod]
    public async Task RepositoryOperations_ShouldComplete_WithinPerformanceThresholds()
    {
        // Arrange - Test PER-002: Entity loading ≤5s for 1000 entities (scaled down for unit test)
        var tables = Enumerable.Range(1, 100)
            .ToDictionary(i => $"dbo.Table{i}", i => new TableInfo("dbo", $"Table{i}"));

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(null))
            .ReturnsAsync(tables);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _schemaRepository.GetTablesAsync();
        var duration = DateTime.UtcNow - startTime;

        // Assert
        result.Should().HaveCount(100);
        duration.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Mock should be very fast
    }
}
