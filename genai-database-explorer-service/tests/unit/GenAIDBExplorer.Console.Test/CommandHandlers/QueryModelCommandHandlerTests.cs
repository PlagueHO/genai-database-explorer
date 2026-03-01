using FluentAssertions;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticModelQuery;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Console.Test.CommandHandlers;

[TestClass]
public class QueryModelCommandHandlerTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider = null!;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider = null!;
    private Mock<ISemanticModelQueryService> _mockQueryService = null!;
    private Mock<IOutputService> _mockOutputService = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<ILogger<ICommandHandler<QueryModelCommandHandlerOptions>>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProject = new Mock<IProject>();
        _mockSemanticModelProvider = new Mock<ISemanticModelProvider>();
        _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
        _mockQueryService = new Mock<ISemanticModelQueryService>();
        _mockOutputService = new Mock<IOutputService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ICommandHandler<QueryModelCommandHandlerOptions>>>();

        var settings = CreateProjectSettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);
    }

    [TestMethod]
    public async Task HandleAsync_WithEmptyQuestion_ShouldOutputError()
    {
        // Arrange
        var handler = CreateHandler();
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "genaidb-test-" + Guid.NewGuid()));
        var options = new QueryModelCommandHandlerOptions(tempDir, "   ");

        // Act
        await handler.HandleAsync(options);

        // Assert
        _mockOutputService.Verify(o => o.WriteError(It.Is<string>(s => s.Contains("empty"))), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_WithValidQuestion_ShouldStreamTokensAndDisplayMetadata()
    {
        // Arrange
        var handler = CreateHandler();
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "genaidb-test-" + Guid.NewGuid()));

        _mockProject.Setup(p => p.LoadProjectConfiguration(It.IsAny<DirectoryInfo>()));
        _mockSemanticModelProvider.Setup(s => s.LoadSemanticModelAsync())
            .ReturnsAsync(CreateEmptySemanticModel());

        var metadataSource = new TaskCompletionSource<SemanticModelQueryResult>();
        var tokens = CreateTokenStream("Hello world");
        var streamingResult = new SemanticModelStreamingQueryResult(tokens, metadataSource);

        var queryResult = new SemanticModelQueryResult(
            Answer: "Hello world",
            ReferencedEntities: new List<SemanticModelSearchResult>
            {
                new("Table", "SalesLT", "Customer", "Customer table", 0.95)
            },
            ResponseRounds: 2,
            InputTokens: 100,
            OutputTokens: 50,
            TotalTokens: 150,
            Duration: TimeSpan.FromSeconds(2.5),
            TerminationReason: QueryTerminationReason.Completed);
        metadataSource.SetResult(queryResult);

        _mockQueryService
            .Setup(s => s.QueryStreamingAsync(It.IsAny<SemanticModelQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamingResult);

        var options = new QueryModelCommandHandlerOptions(tempDir, "What tables store customer data?");

        // Act
        await handler.HandleAsync(options);

        // Assert — verify tokens were streamed
        _mockOutputService.Verify(o => o.Write("Hello"), Times.Once);
        _mockOutputService.Verify(o => o.Write(" world"), Times.Once);

        // Verify metadata display
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("Referenced Entities"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("SalesLT.Customer"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("Query Statistics"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("Response Rounds: 2"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("Completed"))), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_WhenServiceThrowsInvalidOperation_ShouldOutputError()
    {
        // Arrange
        var handler = CreateHandler();
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "genaidb-test-" + Guid.NewGuid()));

        _mockProject.Setup(p => p.LoadProjectConfiguration(It.IsAny<DirectoryInfo>()));
        _mockSemanticModelProvider.Setup(s => s.LoadSemanticModelAsync())
            .ReturnsAsync(CreateEmptySemanticModel());

        _mockQueryService
            .Setup(s => s.QueryStreamingAsync(It.IsAny<SemanticModelQueryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Vector embeddings have not been generated."));

        var options = new QueryModelCommandHandlerOptions(tempDir, "What tables?");

        // Act
        await handler.HandleAsync(options);

        // Assert
        _mockOutputService.Verify(o => o.WriteError(It.Is<string>(s => s.Contains("Vector embeddings"))), Times.Once);
    }

    private QueryModelCommandHandler CreateHandler()
    {
        return new QueryModelCommandHandler(
            _mockProject.Object,
            _mockSemanticModelProvider.Object,
            _mockConnectionProvider.Object,
            _mockQueryService.Object,
            _mockOutputService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);
    }

    private static async IAsyncEnumerable<string> CreateTokenStream(string text)
    {
        var parts = text.Split(' ');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = i == 0 ? parts[i] : " " + parts[i];
            yield return token;
            await Task.Yield();
        }
    }

    private static SemanticModel CreateEmptySemanticModel()
    {
        return new SemanticModel("TestDB", "Test database");
    }

    private static ProjectSettings CreateProjectSettings()
    {
        return new ProjectSettings
        {
            SettingsVersion = new Version("2.0.0"),
            Database = new DatabaseSettings { Name = "TestDB", Description = "Test database" },
            DataDictionary = new DataDictionarySettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings(),
            SemanticModel = new SemanticModelSettings { PersistenceStrategy = "LocalDisk" },
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings(),
            QueryModel = new QueryModelSettings()
        };
    }
}
