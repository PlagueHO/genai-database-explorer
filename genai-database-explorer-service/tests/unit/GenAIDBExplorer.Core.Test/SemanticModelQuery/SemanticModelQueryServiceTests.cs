using FluentAssertions;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.PromptTemplates;
using GenAIDBExplorer.Core.SemanticModelQuery;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.SemanticModelQuery;

[TestClass]
public class SemanticModelQueryServiceTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISemanticModelSearchService> _mockSearchService = null!;
    private Mock<IChatClientFactory> _mockChatClientFactory = null!;
    private Mock<IPromptTemplateParser> _mockPromptTemplateParser = null!;
    private Mock<ILiquidTemplateRenderer> _mockLiquidTemplateRenderer = null!;
    private Mock<ILoggerFactory> _mockLoggerFactory = null!;
    private Mock<ILogger<SemanticModelQueryService>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProject = new Mock<IProject>();
        _mockSearchService = new Mock<ISemanticModelSearchService>();
        _mockChatClientFactory = new Mock<IChatClientFactory>();
        _mockPromptTemplateParser = new Mock<IPromptTemplateParser>();
        _mockLiquidTemplateRenderer = new Mock<ILiquidTemplateRenderer>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<SemanticModelQueryService>>();

        _mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var settings = CreateProjectSettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);
    }

    [TestMethod]
    public void Constructor_ShouldNotThrow()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [TestMethod]
    public async Task QueryAsync_WithEmptyQuestion_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var request = new SemanticModelQueryRequest("");

        // Act & Assert
        await service.Invoking(s => s.QueryAsync(request))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task QueryAsync_WithWhitespaceQuestion_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var request = new SemanticModelQueryRequest("   ");

        // Act & Assert
        await service.Invoking(s => s.QueryAsync(request))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task QueryStreamingAsync_WithEmptyQuestion_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var request = new SemanticModelQueryRequest("");

        // Act & Assert
        await service.Invoking(s => s.QueryStreamingAsync(request))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await service.Invoking(async s => await s.DisposeAsync())
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await service.DisposeAsync();
        await service.Invoking(async s => await s.DisposeAsync())
            .Should().NotThrowAsync();
    }

    private SemanticModelQueryService CreateService()
    {
        return new SemanticModelQueryService(
            _mockProject.Object,
            _mockSearchService.Object,
            _mockChatClientFactory.Object,
            _mockPromptTemplateParser.Object,
            _mockLiquidTemplateRenderer.Object,
            _mockLoggerFactory.Object,
            _mockLogger.Object);
    }

    private static ProjectSettings CreateProjectSettings()
    {
        return new ProjectSettings
        {
            SettingsVersion = new Version("1.0.0"),
            Database = new DatabaseSettings { Name = "TestDB", Description = "Test database" },
            DataDictionary = new DataDictionarySettings(),
            FoundryModels = new FoundryModelsSettings(),
            SemanticModel = new SemanticModelSettings { PersistenceStrategy = "LocalDisk" },
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings(),
            QueryModel = new QueryModelSettings
            {
                AgentName = "test-agent",
                MaxResponseRounds = 5,
                MaxTokenBudget = 50_000,
                TimeoutSeconds = 30,
                DefaultTopK = 3
            }
        };
    }
}
