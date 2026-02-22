using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.PromptTemplates;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GenAIDBExplorer.Core.Tests.SemanticProviders;

/// <summary>
/// Unit tests for <see cref="SemanticDescriptionProvider"/>.
/// Covers T024 (UpdateSemanticDescriptionAsync) and T025 (structured output methods).
/// </summary>
[TestClass]
public class SemanticDescriptionProviderTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<IChatClientFactory> _mockChatClientFactory = null!;
    private Mock<IPromptTemplateParser> _mockPromptTemplateParser = null!;
    private Mock<ILiquidTemplateRenderer> _mockLiquidTemplateRenderer = null!;
    private Mock<ISchemaRepository> _mockSchemaRepository = null!;
    private Mock<ILogger<SemanticDescriptionProvider>> _mockLogger = null!;
    private Mock<IChatClient> _mockChatClient = null!;
    private Mock<IChatClient> _mockStructuredChatClient = null!;
    private SemanticDescriptionProvider _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockProject = new Mock<IProject>();
        _mockChatClientFactory = new Mock<IChatClientFactory>();
        _mockPromptTemplateParser = new Mock<IPromptTemplateParser>();
        _mockLiquidTemplateRenderer = new Mock<ILiquidTemplateRenderer>();
        _mockSchemaRepository = new Mock<ISchemaRepository>();
        _mockLogger = new Mock<ILogger<SemanticDescriptionProvider>>();
        _mockChatClient = new Mock<IChatClient>();
        _mockStructuredChatClient = new Mock<IChatClient>();

        var projectSettings = new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=test;Database=TestDB;",
                Description = "A test database"
            },
            DataDictionary = new DataDictionarySettings(),
            FoundryModels = new FoundryModelsSettings(),
            SemanticModel = new SemanticModelSettings
            {
                MaxDegreeOfParallelism = 1
            },
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration { Directory = "SemanticModel" }
            }
        };

        _mockProject.Setup(p => p.Settings).Returns(projectSettings);
        _mockChatClientFactory.Setup(f => f.CreateChatClient()).Returns(_mockChatClient.Object);
        _mockChatClientFactory.Setup(f => f.CreateStructuredOutputChatClient()).Returns(_mockStructuredChatClient.Object);

        _provider = new SemanticDescriptionProvider(
            _mockProject.Object,
            _mockChatClientFactory.Object,
            _mockPromptTemplateParser.Object,
            _mockLiquidTemplateRenderer.Object,
            _mockSchemaRepository.Object,
            _mockLogger.Object);
    }

    #region Helper Methods

    private static PromptTemplateDefinition CreateTestTemplateDefinition()
    {
        return new PromptTemplateDefinition(
            "test-prompt",
            "Test prompt template",
            new PromptTemplateModelParameters(),
            [
                new PromptTemplateMessage(ChatRole.System, "You are a helpful assistant."),
                new PromptTemplateMessage(ChatRole.User, "Describe the entity: {{ entity_structure }}")
            ]);
    }

    private static IReadOnlyList<ChatMessage> CreateRenderedMessages()
    {
        return
        [
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Describe the entity: schema: dbo\nname: TestTable")
        ];
    }

    private static ChatResponse CreateChatResponse(string text, long inputTokens = 10, long outputTokens = 20)
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = new UsageDetails
            {
                InputTokenCount = inputTokens,
                OutputTokenCount = outputTokens,
                TotalTokenCount = inputTokens + outputTokens
            }
        };
        return response;
    }

    private void SetupPromptTemplateAndRendering()
    {
        var templateDefinition = CreateTestTemplateDefinition();
        var renderedMessages = CreateRenderedMessages();

        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Returns(renderedMessages);
    }

    #endregion

    #region T024: UpdateSemanticDescriptionAsync - Table Tests

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_SetsSemanticDescription()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        var expectedDescription = "This table stores product inventory data.";
        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(expectedDescription));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        table.SemanticDescription.Should().Be(expectedDescription);
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_TracksTokenUsage()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description", 100, 50));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        result.GetTotalInputTokenCount().Should().Be(100);
        result.GetTotalOutputTokenCount().Should().Be(50);
        result.GetTotalTokenCount().Should().Be(150);
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_EmptyResponse_DoesNotSetDescription()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(""));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        table.SemanticDescription.Should().BeNull();
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_ParsesCorrectPromptFile()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        string? capturedFilePath = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Callback<string>(path => capturedFilePath = path)
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Returns(CreateRenderedMessages());

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description"));

        // Act
        await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        capturedFilePath.Should().NotBeNull();
        capturedFilePath.Should().EndWith("describe_semanticmodeltable.prompt");
        capturedFilePath.Should().Contain("PromptTemplates");
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_PassesCorrectVariables()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        IDictionary<string, object?>? capturedVariables = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Callback<PromptTemplateDefinition, IDictionary<string, object?>>((_, vars) => capturedVariables = vars)
            .Returns(CreateRenderedMessages());

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description"));

        // Act
        await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("entity_structure");
        capturedVariables.Should().ContainKey("entity_data");
        capturedVariables.Should().ContainKey("project_description");
        capturedVariables["project_description"].Should().Be("A test database");
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_FetchesSampleData()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product", "Products table");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(
                It.Is<TableInfo>(t => t.SchemaName == "dbo" && t.TableName == "Product"), 5, true))
            .ReturnsAsync([new Dictionary<string, object?> { { "ID", 1 }, { "Name", "Widget" } }]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description"));

        // Act
        await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        _mockSchemaRepository.Verify(r => r.GetSampleTableDataAsync(
            It.Is<TableInfo>(t => t.SchemaName == "dbo" && t.TableName == "Product"), 5, true), Times.Once);
    }

    #endregion

    #region T024: UpdateSemanticDescriptionAsync - View Tests

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_View_SetsSemanticDescription()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "ProductView", "Products view")
        {
            Definition = "SELECT * FROM dbo.Product"
        };
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        // Setup structured output for GetTableListFromViewDefinitionAsync
        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        _mockSchemaRepository
            .Setup(r => r.GetSampleViewDataAsync(It.IsAny<ViewInfo>(), 5, true))
            .ReturnsAsync([]);

        var expectedDescription = "This view provides a summary of products.";
        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(expectedDescription));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, view);

        // Assert
        view.SemanticDescription.Should().Be(expectedDescription);
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_View_FetchesRelatedTables()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "ProductView", "Products view")
        {
            Definition = "SELECT * FROM dbo.Product"
        };
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        semanticModel.Tables.Add(new SemanticModelTable("dbo", "Product", "Products table")
        {
            SemanticDescription = "Already described"
        });

        SetupPromptTemplateAndRendering();

        // Return table list from structured output
        var tableList = new TableList { Tables = [new TableInfo("dbo", "Product")] };
        var tableListJson = JsonSerializer.Serialize(tableList);
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        _mockSchemaRepository
            .Setup(r => r.GetSampleViewDataAsync(It.IsAny<ViewInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("View description"));

        // Act
        await _provider.UpdateSemanticDescriptionAsync(semanticModel, view);

        // Assert
        _mockChatClientFactory.Verify(f => f.CreateStructuredOutputChatClient(), Times.Once);
    }

    #endregion

    #region T024: UpdateSemanticDescriptionAsync - StoredProcedure Tests

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_StoredProcedure_SetsDescription()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts",
            "CREATE PROCEDURE dbo.GetProducts AS SELECT * FROM Product", "@CategoryId INT");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        // Setup structured output for GetTableListFromStoredProcedureDefinitionAsync
        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        var expectedDescription = "This stored procedure retrieves products by category.";
        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(expectedDescription));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, sp);

        // Assert
        sp.SemanticDescription.Should().Be(expectedDescription);
    }

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_StoredProcedure_IncludesDefinitionAndParameters()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts",
            "CREATE PROCEDURE dbo.GetProducts AS SELECT * FROM Product", "@CategoryId INT");
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        IDictionary<string, object?>? capturedVariables = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Callback<PromptTemplateDefinition, IDictionary<string, object?>>((_, vars) => capturedVariables = vars)
            .Returns(CreateRenderedMessages());

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("SP Description"));

        // Act
        await _provider.UpdateSemanticDescriptionAsync(semanticModel, sp);

        // Assert - The second call captures the SP description variables (first call is for table list)
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("entity_definition");
        capturedVariables.Should().ContainKey("entity_parameters");
        capturedVariables["entity_definition"].Should().Be("CREATE PROCEDURE dbo.GetProducts AS SELECT * FROM Product");
        capturedVariables["entity_parameters"].Should().Be("@CategoryId INT");
    }

    #endregion

    #region T024: UpdateSemanticDescriptionAsync - Multiple Entities

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_MultipleEntities_ProcessesAll()
    {
        // Arrange
        var tables = new List<SemanticModelTable>
        {
            new("dbo", "Product", "Products"),
            new("dbo", "Customer", "Customers"),
            new("dbo", "Order", "Orders")
        };
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description"));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, tables);

        // Assert
        result.Should().HaveCount(3);
        tables.Should().OnlyContain(t => t.SemanticDescription == "Description");
    }

    #endregion

    #region T024: UpdateTableSemanticDescriptionAsync

    [TestMethod]
    public async Task UpdateTableSemanticDescriptionAsync_WithTableList_OnlyUpdatesTablesWithoutDescription()
    {
        // Arrange
        var tableWithDesc = new SemanticModelTable("dbo", "Product", "Products");
        tableWithDesc.SetSemanticDescription("Already described");

        var tableWithoutDesc = new SemanticModelTable("dbo", "Customer", "Customers");

        var semanticModel = new SemanticModel("TestDB", "test-connection");
        semanticModel.Tables.Add(tableWithDesc);
        semanticModel.Tables.Add(tableWithoutDesc);

        var tableList = new TableList
        {
            Tables = [new TableInfo("dbo", "Product"), new TableInfo("dbo", "Customer")]
        };

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("New description"));

        // Act
        var result = await _provider.UpdateTableSemanticDescriptionAsync(semanticModel, tableList);

        // Assert
        tableWithDesc.SemanticDescription.Should().Be("Already described");
        tableWithoutDesc.SemanticDescription.Should().Be("New description");
    }

    #endregion

    #region T025: GetTableListFromViewDefinitionAsync

    [TestMethod]
    public async Task GetTableListFromViewDefinitionAsync_ReturnsTableList()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "ProductView", "Products view")
        {
            Definition = "SELECT p.*, c.Name FROM dbo.Product p JOIN dbo.Category c ON p.CategoryId = c.Id"
        };
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        var expectedTableList = new TableList
        {
            Tables = [new TableInfo("dbo", "Product"), new TableInfo("dbo", "Category")]
        };
        var tableListJson = JsonSerializer.Serialize(expectedTableList);

        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        var result = await _provider.GetTableListFromViewDefinitionAsync(semanticModel, view);

        // Assert
        result.Tables.Should().HaveCount(2);
        result.Tables.Should().Contain(t => t.SchemaName == "dbo" && t.TableName == "Product");
        result.Tables.Should().Contain(t => t.SchemaName == "dbo" && t.TableName == "Category");
    }

    [TestMethod]
    public async Task GetTableListFromViewDefinitionAsync_UsesStructuredOutputClient()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "TestView") { Definition = "SELECT 1" };
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromViewDefinitionAsync(semanticModel, view);

        // Assert
        _mockChatClientFactory.Verify(f => f.CreateStructuredOutputChatClient(), Times.Once);
        _mockChatClientFactory.Verify(f => f.CreateChatClient(), Times.Never);
    }

    [TestMethod]
    public async Task GetTableListFromViewDefinitionAsync_ParsesCorrectPromptFile()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "TestView") { Definition = "SELECT 1" };
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        string? capturedFilePath = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Callback<string>(path => capturedFilePath = path)
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Returns(CreateRenderedMessages());

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromViewDefinitionAsync(semanticModel, view);

        // Assert
        capturedFilePath.Should().NotBeNull();
        capturedFilePath.Should().EndWith("get_tables_from_view_definition.prompt");
    }

    [TestMethod]
    public async Task GetTableListFromViewDefinitionAsync_EmptyResponse_ReturnsEmptyTableList()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "TestView") { Definition = "SELECT 1" };
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(""));

        // Act
        var result = await _provider.GetTableListFromViewDefinitionAsync(semanticModel, view);

        // Assert
        result.Tables.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetTableListFromViewDefinitionAsync_PassesViewDefinitionAsVariable()
    {
        // Arrange
        var viewDefinition = "SELECT p.Name FROM dbo.Product p";
        var view = new SemanticModelView("dbo", "TestView") { Definition = viewDefinition };
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        IDictionary<string, object?>? capturedVariables = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Callback<PromptTemplateDefinition, IDictionary<string, object?>>((_, vars) => capturedVariables = vars)
            .Returns(CreateRenderedMessages());

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromViewDefinitionAsync(semanticModel, view);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("entity_definition");
        capturedVariables["entity_definition"].Should().Be(viewDefinition);
    }

    #endregion

    #region T025: GetTableListFromStoredProcedureDefinitionAsync

    [TestMethod]
    public async Task GetTableListFromStoredProcedureDefinitionAsync_ReturnsTableList()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts",
            "CREATE PROCEDURE dbo.GetProducts AS SELECT * FROM dbo.Product JOIN dbo.Category ON ...");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        var expectedTableList = new TableList
        {
            Tables = [new TableInfo("dbo", "Product"), new TableInfo("dbo", "Category")]
        };
        var tableListJson = JsonSerializer.Serialize(expectedTableList);

        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        var result = await _provider.GetTableListFromStoredProcedureDefinitionAsync(semanticModel, sp);

        // Assert
        result.Tables.Should().HaveCount(2);
        result.Tables.Should().Contain(t => t.SchemaName == "dbo" && t.TableName == "Product");
    }

    [TestMethod]
    public async Task GetTableListFromStoredProcedureDefinitionAsync_UsesStructuredOutputClient()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts", "CREATE PROCEDURE...");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromStoredProcedureDefinitionAsync(semanticModel, sp);

        // Assert
        _mockChatClientFactory.Verify(f => f.CreateStructuredOutputChatClient(), Times.Once);
    }

    [TestMethod]
    public async Task GetTableListFromStoredProcedureDefinitionAsync_ParsesCorrectPromptFile()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts", "CREATE PROCEDURE...");
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        string? capturedFilePath = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Callback<string>(path => capturedFilePath = path)
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Returns(CreateRenderedMessages());

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromStoredProcedureDefinitionAsync(semanticModel, sp);

        // Assert
        capturedFilePath.Should().NotBeNull();
        capturedFilePath.Should().EndWith("get_tables_from_storedprocedure_definition.prompt");
    }

    [TestMethod]
    public async Task GetTableListFromStoredProcedureDefinitionAsync_EmptyResponse_ReturnsEmptyTableList()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts", "CREATE PROCEDURE...");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(""));

        // Act
        var result = await _provider.GetTableListFromStoredProcedureDefinitionAsync(semanticModel, sp);

        // Assert
        result.Tables.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetTableListFromStoredProcedureDefinitionAsync_PassesSPDefinitionAsVariable()
    {
        // Arrange
        var spDefinition = "CREATE PROCEDURE dbo.GetProducts AS SELECT * FROM dbo.Product";
        var sp = new SemanticModelStoredProcedure("dbo", "GetProducts", spDefinition);
        var semanticModel = new SemanticModel("TestDB", "test-connection");
        IDictionary<string, object?>? capturedVariables = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Callback<PromptTemplateDefinition, IDictionary<string, object?>>((_, vars) => capturedVariables = vars)
            .Returns(CreateRenderedMessages());

        var tableListJson = JsonSerializer.Serialize(new TableList { Tables = [] });
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(tableListJson));

        // Act
        await _provider.GetTableListFromStoredProcedureDefinitionAsync(semanticModel, sp);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("entity_definition");
        capturedVariables["entity_definition"].Should().Be(spDefinition);
    }

    #endregion

    #region T024: ProcessResult Label Tests

    [TestMethod]
    public async Task UpdateSemanticDescriptionAsync_Table_ResultLabel_IsChatCompletion()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "Product");
        var semanticModel = new SemanticModel("TestDB", "test-connection");

        SetupPromptTemplateAndRendering();

        _mockSchemaRepository
            .Setup(r => r.GetSampleTableDataAsync(It.IsAny<TableInfo>(), 5, true))
            .ReturnsAsync([]);

        _mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("Description"));

        // Act
        var result = await _provider.UpdateSemanticDescriptionAsync(semanticModel, table);

        // Assert
        result.Should().HaveCount(1);
        result.GetTotalTokenCount("ChatCompletion").Should().Be(30);
    }

    #endregion
}
