using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class InitCommandHandlerTests
{
    private Mock<ILogger<ICommandHandler>> _loggerMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IProjectFactory> _projectFactoryMock;
    private InitCommandHandler _initCommandHandler;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ICommandHandler>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _projectFactoryMock = new Mock<IProjectFactory>();
        _initCommandHandler = new InitCommandHandler(_projectFactoryMock.Object, _serviceProviderMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public void Handle_ShouldLogInformation_WhenProjectDirectoryIsValid()
    {
        // Arrange
        var projectDirectory = new DirectoryInfo("TestProject");
        if (!projectDirectory.Exists)
        {
            projectDirectory.Create();
        }

        // Act
        _initCommandHandler.Handle(projectDirectory);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains($"Initializing project at '{projectDirectory.FullName}'.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        // Cleanup
        projectDirectory.Delete(true);
    }

    [TestMethod]
    public void Handle_ShouldWarnUser_WhenProjectDirectoryIsNotEmpty()
    {
        // Arrange
        var projectDirectory = new DirectoryInfo("TestProject");
        if (!projectDirectory.Exists)
        {
            projectDirectory.Create();
        }
        File.Create(Path.Combine(projectDirectory.FullName, "test.txt")).Dispose();

        using var consoleOutput = new ConsoleOutput();

        // Act
        _initCommandHandler.Handle(projectDirectory);

        // Assert
        consoleOutput.GetOutput().Should().Contain("The project folder is not empty. Please specify an empty folder.");

        // Cleanup
        projectDirectory.Delete(true);
    }

    [TestMethod]
    public void Handle_ShouldInitializeProject_WhenProjectDirectoryIsEmpty()
    {
        // Arrange
        var projectDirectory = new DirectoryInfo("TestProject");
        if (!projectDirectory.Exists)
        {
            projectDirectory.Create();
        }

        var defaultProjectDirectory = new DirectoryInfo("DefaultProject");
        if (!defaultProjectDirectory.Exists)
        {
            defaultProjectDirectory.Create();
        }
        File.Create(Path.Combine(defaultProjectDirectory.FullName, "default.txt")).Dispose();

        using var consoleOutput = new ConsoleOutput();

        // Act
        _initCommandHandler.Handle(projectDirectory);

        // Assert
        consoleOutput.GetOutput().Should().Contain($"Project initialized successfully in '{projectDirectory.FullName}'.");
        projectDirectory.GetFiles().Any(file => file.Name == "default.txt").Should().BeTrue();

        // Cleanup
        projectDirectory.Delete(true);
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