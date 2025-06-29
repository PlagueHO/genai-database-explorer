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
public class ExtractModelCommandHandlerTests
{
    private Mock<IProject> _mockProject;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<ICommandHandler<ExtractModelCommandHandlerOptions>>> _mockLogger;
    private Mock<IOutputService> _mockOutputService;
    private ExtractModelCommandHandler _handler;

    [TestInitialize]
    public void SetUp()
    {
        // Arrange: Set up mock dependencies
        _mockProject = new Mock<IProject>();
        _mockSemanticModelProvider = new Mock<ISemanticModelProvider>();
        _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ICommandHandler<ExtractModelCommandHandlerOptions>>>();
        _mockOutputService = new Mock<IOutputService>();

        // Arrange: Initialize the handler with mock dependencies
        _handler = new ExtractModelCommandHandler(
            _mockProject.Object,
            _mockConnectionProvider.Object,
            _mockSemanticModelProvider.Object,
            _mockOutputService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );
    }

    [TestMethod]
    public async Task HandleAsync_ShouldExtractAndSaveSemanticModel_WhenProjectPathIsValid()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new ExtractModelCommandHandlerOptions(projectPath);

        var semanticModel = new SemanticModel("TestModel", "TestSource");

        _mockSemanticModelProvider.Setup(p => p.ExtractSemanticModelAsync())
            .ReturnsAsync(semanticModel);

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockProject.Verify(p => p.LoadProjectConfiguration(projectPath), Times.Once);
        _mockSemanticModelProvider.Verify(p => p.ExtractSemanticModelAsync(), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Extracting semantic model for project. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Saving semantic model. '{Path.Combine(projectPath.FullName, semanticModel.Name)}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Semantic model extraction complete. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldNotLogCompletion_WhenExtractSemanticModelAsyncThrowsException()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new ExtractModelCommandHandlerOptions(projectPath);

        var exceptionMessage = "Database connection failed";
        _mockSemanticModelProvider.Setup(p => p.ExtractSemanticModelAsync())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);

        _mockProject.Verify(p => p.LoadProjectConfiguration(projectPath), Times.Once);
        _mockSemanticModelProvider.Verify(p => p.ExtractSemanticModelAsync(), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Semantic model extraction complete. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenCommandOptionsIsNull()
    {
        // Arrange
        ExtractModelCommandHandlerOptions commandOptions = null;

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.LoadProjectConfiguration(It.IsAny<DirectoryInfo>()), Times.Never);
        _mockSemanticModelProvider.Verify(p => p.ExtractSemanticModelAsync(), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenProjectPathIsNull()
    {
        // Arrange
        var commandOptions = new ExtractModelCommandHandlerOptions(null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.LoadProjectConfiguration(It.IsAny<DirectoryInfo>()), Times.Never);
        _mockSemanticModelProvider.Verify(p => p.ExtractSemanticModelAsync(), Times.Never);
    }
}