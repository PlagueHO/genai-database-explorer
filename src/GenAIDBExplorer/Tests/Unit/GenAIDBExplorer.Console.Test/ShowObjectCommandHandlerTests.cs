using FluentAssertions;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class ShowObjectCommandHandlerTests
{
    private Mock<IProject> _mockProject;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<ICommandHandler<ShowObjectCommandHandlerOptions>>> _mockLogger;
    private Mock<IOutputService> _mockOutputService;
    private ShowObjectCommandHandler _handler;

    [TestInitialize]
    public void SetUp()
    {
        // Arrange: Set up mock dependencies
        _mockProject = new Mock<IProject>();
        _mockSemanticModelProvider = new Mock<ISemanticModelProvider>();
        _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ICommandHandler<ShowObjectCommandHandlerOptions>>>();
        _mockOutputService = new Mock<IOutputService>();

        // Mock the ProjectSettings
        var mockProjectSettings = new ProjectSettings
        {
            Database = new DatabaseSettings { Name = "TestDatabase" },
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            OpenAIService = new OpenAIServiceSettings()
        };

        _mockProject.Setup(p => p.Settings).Returns(mockProjectSettings);

        // Arrange: Initialize the handler with mock dependencies
        _handler = new ShowObjectCommandHandler(
            _mockProject.Object,
            _mockSemanticModelProvider.Object,
            _mockConnectionProvider.Object,
            _mockOutputService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );
    }

    [TestMethod]
    public async Task HandleAsync_ShouldShowTableDetails_WhenObjectTypeIsTable()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new ShowObjectCommandHandlerOptions(projectPath, "dbo", "TestTable", "table");

        var semanticModel = new SemanticModel("TestModel", "TestSource");
        var table = new SemanticModelTable("dbo", "TestTable");
        semanticModel.AddTable(table);

        _mockSemanticModelProvider.Setup(p => p.LoadSemanticModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockSemanticModelProvider.Verify(p => p.LoadSemanticModelAsync(It.Is<DirectoryInfo>(d => d.FullName == Path.Combine(projectPath.FullName, "TestDatabase"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("TestTable"))), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldShowViewDetails_WhenObjectTypeIsView()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new ShowObjectCommandHandlerOptions(projectPath, "dbo", "TestView", "view");

        var semanticModel = new SemanticModel("TestModel", "TestSource");
        var view = new SemanticModelView("dbo", "TestView");
        semanticModel.AddView(view);

        _mockSemanticModelProvider.Setup(p => p.LoadSemanticModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockSemanticModelProvider.Verify(p => p.LoadSemanticModelAsync(It.Is<DirectoryInfo>(d => d.FullName == Path.Combine(projectPath.FullName, "TestDatabase"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("TestView"))), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldShowStoredProcedureDetails_WhenObjectTypeIsStoredProcedure()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new ShowObjectCommandHandlerOptions(projectPath, "dbo", "TestStoredProcedure", "storedprocedure");

        var semanticModel = new SemanticModel("TestModel", "TestSource");
        var storedProcedure = new SemanticModelStoredProcedure("dbo", "TestStoredProcedure", "CREATE PROCEDURE TestStoredProcedure AS BEGIN SELECT 1 END");
        semanticModel.AddStoredProcedure(storedProcedure);

        _mockSemanticModelProvider.Setup(p => p.LoadSemanticModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(semanticModel);

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockSemanticModelProvider.Verify(p => p.LoadSemanticModelAsync(It.Is<DirectoryInfo>(d => d.FullName == Path.Combine(projectPath.FullName, "TestDatabase"))), Times.Once);
        _mockOutputService.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("TestStoredProcedure"))), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenCommandOptionsIsNull()
    {
        // Arrange
        ShowObjectCommandHandlerOptions commandOptions = null;

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockSemanticModelProvider.Verify(p => p.LoadSemanticModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenProjectPathIsNull()
    {
        // Arrange
        var commandOptions = new ShowObjectCommandHandlerOptions(null, "dbo", "TestTable", "table");

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockSemanticModelProvider.Verify(p => p.LoadSemanticModelAsync(It.IsAny<DirectoryInfo>()), Times.Never);
    }
}