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
public class InitProjectCommandHandlerTests
{
    private Mock<IProject> _mockProject;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider;
    private Mock<ISemanticDescriptionProvider> _mockSemanticDescriptionProvider;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>> _mockLogger;
    private InitProjectCommandHandler _handler;

    [TestInitialize]
    public void SetUp()
    {
        // Arrange: Set up mock dependencies
        _mockProject = new Mock<IProject>();
        _mockSemanticModelProvider = new Mock<ISemanticModelProvider>();
        _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
        _mockSemanticDescriptionProvider = new Mock<ISemanticDescriptionProvider>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>>();

        // Arrange: Initialize the handler with mock dependencies
        _handler = new InitProjectCommandHandler(
            _mockProject.Object,
            _mockSemanticModelProvider.Object,
            _mockConnectionProvider.Object,
            _mockSemanticDescriptionProvider.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );
    }
}