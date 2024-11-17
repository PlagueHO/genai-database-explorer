using FluentAssertions;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class InitCommandHandlerTests
{
    private Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>> _loggerMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IProject> _projectMock;
    private Mock<IDatabaseConnectionProvider> _sqlConnectionProviderMock;
    private Mock<ISemanticModelProvider> _semanticModelProviderMock;
    private Mock<ISemanticDescriptionProvider> _semanticDescriptionProviderMock;
    private InitProjectCommandHandler _initCommandHandler;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _projectMock = new Mock<IProject>();
        _sqlConnectionProviderMock = new Mock<IDatabaseConnectionProvider>();
        _semanticModelProviderMock = new Mock<ISemanticModelProvider>();
        _semanticDescriptionProviderMock = new Mock<ISemanticDescriptionProvider>();
        _initCommandHandler = new InitProjectCommandHandler(
            _projectMock.Object,
            _semanticModelProviderMock.Object,
            _sqlConnectionProviderMock.Object,
            _semanticDescriptionProviderMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object
        );
    }

    [TestMethod]
    public async Task HandleAsync_ShouldLogInformation_WhenProjectDirectoryIsValid()
    {
        // Arrange
        var projectPath = new DirectoryInfo("TestProject");
        if (!projectPath.Exists)
        {
            projectPath.Create();
        }

        var options = new InitProjectCommandHandlerOptions(projectPath);

        // Act
        await _initCommandHandler.HandleAsync(options);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains($"Initializing project at '{projectPath.FullName}'.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        // Cleanup
        projectPath.Delete(true);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldWarnUser_WhenProjectDirectoryIsNotEmpty()
    {
        // Arrange
        var projectPath = new DirectoryInfo("TestProject");
        if (!projectPath.Exists)
        {
            projectPath.Create();
        }
        File.Create(Path.Combine(projectPath.FullName, "test.txt")).Dispose();

        var options = new InitProjectCommandHandlerOptions(projectPath);

        using var consoleOutput = new ConsoleOutput();

        // Act
        await _initCommandHandler.HandleAsync(options);

        // Assert
        consoleOutput.GetOutput().Should().Contain("The project folder is not empty. Please specify an empty folder.");

        // Cleanup
        projectPath.Delete(true);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldInitializeProject_WhenProjectDirectoryIsEmpty()
    {
        // Arrange
        var projectPath = new DirectoryInfo("TestProject");
        if (!projectPath.Exists)
        {
            projectPath.Create();
        }

        var defaultProjectDirectory = new DirectoryInfo("DefaultProject");
        if (!defaultProjectDirectory.Exists)
        {
            defaultProjectDirectory.Create();
        }
        File.Create(Path.Combine(defaultProjectDirectory.FullName, "default.txt")).Dispose();

        var options = new InitProjectCommandHandlerOptions(projectPath);

        using var consoleOutput = new ConsoleOutput();

        // Act
        await _initCommandHandler.HandleAsync(options);

        // Assert
        consoleOutput.GetOutput().Should().Contain($"Project initialized successfully in '{projectPath.FullName}'.");
        projectPath.GetFiles().Any(file => file.Name == "default.txt").Should().BeTrue();

        // Cleanup
        projectPath.Delete(true);
        defaultProjectDirectory.Delete(true);
    }
}

public class ConsoleOutput : IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;

    public ConsoleOutput()
    {
        _stringWriter = new StringWriter();
        _originalOutput = System.Console.Out;
        System.Console.SetOut(_stringWriter);
    }

    public string GetOutput()
    {
        return _stringWriter.ToString();
    }

    public void Dispose()
    {
        System.Console.SetOut(_originalOutput);
        _stringWriter.Dispose();
    }
}