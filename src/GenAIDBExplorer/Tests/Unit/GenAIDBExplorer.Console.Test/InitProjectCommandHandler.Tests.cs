using FluentAssertions;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class InitProjectCommandHandlerTests
{
    private Mock<IProject> _mockProject;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>> _mockLogger;
    private Mock<IOutputService> _mockOutputService;
    private InitProjectCommandHandler _handler;

    [TestInitialize]
    public void SetUp()
    {
        // Arrange: Set up mock dependencies
        _mockProject = new Mock<IProject>();
        _mockSemanticModelProvider = new Mock<ISemanticModelProvider>();
        _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>>();
        _mockOutputService = new Mock<IOutputService>();

        // Arrange: Initialize the handler with mock dependencies
        _handler = new InitProjectCommandHandler(
            _mockProject.Object,
            _mockSemanticModelProvider.Object,
            _mockConnectionProvider.Object,
            _mockOutputService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );
    }

    [TestMethod]
    public async Task HandleAsync_ShouldInitializeProjectDirectory_WhenProjectPathIsValid()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new InitProjectCommandHandlerOptions(projectPath);

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockProject.Verify(p => p.InitializeProjectDirectory(projectPath), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Initializing project. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Project initialized successfully. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldCatchExceptionAndNotLogCompletion_WhenInitializeProjectDirectoryThrowsException()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new InitProjectCommandHandlerOptions(projectPath);

        var exceptionMessage = "Directory is not empty";
        _mockProject.Setup(p => p.InitializeProjectDirectory(projectPath))
                    .Throws(new Exception(exceptionMessage));

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert
        _mockProject.Verify(p => p.InitializeProjectDirectory(projectPath), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(v => v.ToString().Contains("InitializeProjectComplete")),
                null,
                It.IsAny<Func<object, Exception, string>>()),
            Times.Never);

        _mockOutputService.Verify(o => o.WriteError(It.Is<string>(s => s.Contains(exceptionMessage))), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenCommandOptionsIsNull()
    {
        // Arrange
        InitProjectCommandHandlerOptions commandOptions = null;

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.InitializeProjectDirectory(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenProjectPathIsNull()
    {
        // Arrange
        var commandOptions = new InitProjectCommandHandlerOptions(null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.InitializeProjectDirectory(It.IsAny<DirectoryInfo>()), Times.Never);
    }
}