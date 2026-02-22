using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.DataDictionary;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GenAIDBExplorer.Core.Tests.DataDictionary;

/// <summary>
/// Unit tests for <see cref="DataDictionaryProvider"/>.
/// Covers T030 — mocked IChatClient with structured output for data dictionary extraction.
/// </summary>
[TestClass]
public class DataDictionaryProviderTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<IChatClientFactory> _mockChatClientFactory = null!;
    private Mock<IPromptTemplateParser> _mockPromptTemplateParser = null!;
    private Mock<ILiquidTemplateRenderer> _mockLiquidTemplateRenderer = null!;
    private Mock<ILogger<DataDictionaryProvider>> _mockLogger = null!;
    private Mock<IChatClient> _mockStructuredChatClient = null!;
    private DataDictionaryProvider _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockProject = new Mock<IProject>();
        _mockChatClientFactory = new Mock<IChatClientFactory>();
        _mockPromptTemplateParser = new Mock<IPromptTemplateParser>();
        _mockLiquidTemplateRenderer = new Mock<ILiquidTemplateRenderer>();
        _mockLogger = new Mock<ILogger<DataDictionaryProvider>>();
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
        _mockChatClientFactory.Setup(f => f.CreateStructuredOutputChatClient()).Returns(_mockStructuredChatClient.Object);

        _provider = new DataDictionaryProvider(
            _mockProject.Object,
            _mockChatClientFactory.Object,
            _mockPromptTemplateParser.Object,
            _mockLiquidTemplateRenderer.Object,
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
                new PromptTemplateMessage(ChatRole.User, "Extract table: {{ entity_markdown }}")
            ]);
    }

    private static IReadOnlyList<ChatMessage> CreateRenderedMessages()
    {
        return
        [
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Extract table: # Test Table\n| Column | Type |")
        ];
    }

    private static ChatResponse CreateChatResponse(string text)
    {
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = new UsageDetails
            {
                InputTokenCount = 10,
                OutputTokenCount = 20,
                TotalTokenCount = 30
            }
        };
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

    private static string CreateTableDataDictionaryJson(string schemaName = "dbo", string tableName = "TestTable")
    {
        return JsonSerializer.Serialize(new
        {
            SchemaName = schemaName,
            TableName = tableName,
            Description = "Test table description",
            Details = "Test details",
            AdditionalInformation = "Additional info",
            Columns = new[]
            {
                new { ColumnName = "Id", Type = "int", Size = (int?)null, Description = "Primary key", NotUsed = false },
                new { ColumnName = "Name", Type = "nvarchar", Size = (int?)100, Description = "Name column", NotUsed = false }
            }
        });
    }

    #endregion

    #region GetTableFromMarkdownAsync Tests

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_ReturnsTableDataDictionary()
    {
        // Arrange
        var markdownContent = "# Test Table\n| Column | Type |\n| Id | int |";

        SetupPromptTemplateAndRendering();

        var responseJson = CreateTableDataDictionaryJson();
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(responseJson));

        // Act
        var result = await _provider.GetTableFromMarkdownAsync(markdownContent);

        // Assert
        result.SchemaName.Should().Be("dbo");
        result.TableName.Should().Be("TestTable");
        result.Description.Should().Be("Test table description");
        result.Columns.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_UsesStructuredOutputClient()
    {
        // Arrange
        SetupPromptTemplateAndRendering();

        var responseJson = CreateTableDataDictionaryJson();
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(responseJson));

        // Act
        await _provider.GetTableFromMarkdownAsync("# Test");

        // Assert
        _mockChatClientFactory.Verify(f => f.CreateStructuredOutputChatClient(), Times.Once);
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_ParsesCorrectPromptFile()
    {
        // Arrange
        string? capturedFilePath = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Callback<string>(path => capturedFilePath = path)
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Returns(CreateRenderedMessages());

        var responseJson = CreateTableDataDictionaryJson();
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(responseJson));

        // Act
        await _provider.GetTableFromMarkdownAsync("# Test");

        // Assert
        capturedFilePath.Should().NotBeNull();
        capturedFilePath.Should().EndWith("get_table_from_data_dictionary_markdown.prompt");
        capturedFilePath.Should().Contain("PromptTemplates");
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_PassesMarkdownAsVariable()
    {
        // Arrange
        var markdownContent = "# My Table\n| Column | Type | Description |";
        IDictionary<string, object?>? capturedVariables = null;

        var templateDefinition = CreateTestTemplateDefinition();
        _mockPromptTemplateParser
            .Setup(p => p.ParseFromFile(It.IsAny<string>()))
            .Returns(templateDefinition);

        _mockLiquidTemplateRenderer
            .Setup(r => r.RenderMessages(templateDefinition, It.IsAny<IDictionary<string, object?>>()))
            .Callback<PromptTemplateDefinition, IDictionary<string, object?>>((_, vars) => capturedVariables = vars)
            .Returns(CreateRenderedMessages());

        var responseJson = CreateTableDataDictionaryJson();
        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(responseJson));

        // Act
        await _provider.GetTableFromMarkdownAsync(markdownContent);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("entity_markdown");
        capturedVariables["entity_markdown"].Should().Be(markdownContent);
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_EmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(""));

        // Act
        Func<Task> act = async () => await _provider.GetTableFromMarkdownAsync("# Test");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*extract table structure*");
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        SetupPromptTemplateAndRendering();

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse("not valid json"));

        // Act
        Func<Task> act = async () => await _provider.GetTableFromMarkdownAsync("# Test");

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [TestMethod]
    public async Task GetTableFromMarkdownAsync_WithNotUsedColumn_DeserializesCorrectly()
    {
        // Arrange
        SetupPromptTemplateAndRendering();

        var responseJson = JsonSerializer.Serialize(new
        {
            SchemaName = "dbo",
            TableName = "TestTable",
            Description = "Test",
            Details = "",
            AdditionalInformation = "",
            Columns = new[]
            {
                new { ColumnName = "ActiveFlag", Type = "bit", Size = (int?)null, Description = "NOT USED", NotUsed = true }
            }
        });

        _mockStructuredChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateChatResponse(responseJson));

        // Act
        var result = await _provider.GetTableFromMarkdownAsync("# Test");

        // Assert
        result.Columns.Should().HaveCount(1);
        result.Columns[0].NotUsed.Should().BeTrue();
    }

    #endregion
}
