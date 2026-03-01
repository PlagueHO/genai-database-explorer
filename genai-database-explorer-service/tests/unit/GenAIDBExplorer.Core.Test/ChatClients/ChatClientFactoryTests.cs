using Azure.AI.Projects;
using FluentAssertions;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Test.ChatClients;

[TestClass]
public class ChatClientFactoryTests
{
    private static IProject CreateMockProject(
        AuthenticationType authType = AuthenticationType.ApiKey,
        string? endpoint = "https://test.services.ai.azure.com/api/projects/testproject",
        string? apiKey = "test-api-key",
        string? chatDeploymentName = "gpt-4o",
        string? embeddingDeploymentName = "text-embedding-3-small",
        string? tenantId = null)
    {
        var projectMock = new Mock<IProject>();
        var settings = new ProjectSettings
        {
            SettingsVersion = new Version(2, 0),
            Database = new DatabaseSettings(),
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings
            {
                Default = new MicrosoftFoundryDefaultSettings
                {
                    AuthenticationType = authType,
                    Endpoint = endpoint,
                    ApiKey = apiKey,
                    TenantId = tenantId
                },
                ChatCompletion = new ChatCompletionDeploymentSettings
                {
                    DeploymentName = chatDeploymentName
                },
                Embedding = new EmbeddingDeploymentSettings
                {
                    DeploymentName = embeddingDeploymentName
                }
            }
        };

        projectMock.Setup(p => p.Settings).Returns(settings);
        return projectMock.Object;
    }

    #region CreateChatClient

    [TestMethod]
    public void CreateChatClient_WithApiKeyAuth_ShouldReturnIChatClient()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.ApiKey);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    [TestMethod]
    public void CreateChatClient_WithEntraIdAuth_ShouldReturnIChatClient()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.EntraIdAuthentication);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    [TestMethod]
    public void CreateChatClient_WithProjectEndpoint_ShouldReturnIChatClient()
    {
        // Arrange — valid Foundry project endpoint with API key auth (T023)
        var project = CreateMockProject(
            authType: AuthenticationType.ApiKey,
            endpoint: "https://test.services.ai.azure.com/api/projects/testproject");
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    [TestMethod]
    public void CreateChatClient_MissingEndpoint_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var project = CreateMockProject(endpoint: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateChatClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint*");
    }

    [TestMethod]
    public void CreateChatClient_MissingDeploymentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var project = CreateMockProject(chatDeploymentName: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateChatClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment*");
    }

    [TestMethod]
    public void CreateChatClient_ApiKeyAuth_MissingKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var project = CreateMockProject(
            authType: AuthenticationType.ApiKey,
            apiKey: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateChatClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [TestMethod]
    public void CreateChatClient_LogsProjectEndpoint()
    {
        // Arrange — verify logging of project endpoint at creation time (T025)
        var project = CreateMockProject(authType: AuthenticationType.ApiKey);
        var loggerMock = new Mock<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, loggerMock.Object);

        // Act
        factory.CreateChatClient();

        // Assert — verify that the factory logged a message containing the endpoint
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("test.services.ai.azure.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CreateStructuredOutputChatClient

    [TestMethod]
    public void CreateStructuredOutputChatClient_WithApiKeyAuth_ShouldReturnIChatClient()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.ApiKey);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateStructuredOutputChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    #endregion

    #region CreateEmbeddingGenerator

    [TestMethod]
    public void CreateEmbeddingGenerator_WithApiKeyAuth_ShouldReturnEmbeddingGenerator()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.ApiKey);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var generator = factory.CreateEmbeddingGenerator();

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeAssignableTo<IEmbeddingGenerator<string, Embedding<float>>>();
    }

    [TestMethod]
    public void CreateEmbeddingGenerator_WithEntraIdAuth_ShouldReturnEmbeddingGenerator()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.EntraIdAuthentication);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var generator = factory.CreateEmbeddingGenerator();

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeAssignableTo<IEmbeddingGenerator<string, Embedding<float>>>();
    }

    [TestMethod]
    public void CreateEmbeddingGenerator_WithProjectEndpoint_ShouldReturnEmbeddingGenerator()
    {
        // Arrange — valid Foundry project endpoint (T024)
        var project = CreateMockProject(
            authType: AuthenticationType.ApiKey,
            endpoint: "https://test.services.ai.azure.com/api/projects/testproject");
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var generator = factory.CreateEmbeddingGenerator();

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeAssignableTo<IEmbeddingGenerator<string, Embedding<float>>>();
    }

    [TestMethod]
    public void CreateEmbeddingGenerator_MissingDeploymentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var project = CreateMockProject(embeddingDeploymentName: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateEmbeddingGenerator();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment*");
    }

    #endregion

    #region EntraId With TenantId

    [TestMethod]
    public void CreateChatClient_EntraIdWithTenantId_ShouldReturnIChatClient()
    {
        // Arrange
        var project = CreateMockProject(
            authType: AuthenticationType.EntraIdAuthentication,
            tenantId: "12345678-1234-1234-1234-123456789012");
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    #endregion

    #region GetProjectClient

    [TestMethod]
    public void GetProjectClient_WithEntraIdAuth_ShouldReturnAIProjectClient()
    {
        // Arrange
        var project = CreateMockProject(authType: AuthenticationType.EntraIdAuthentication);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var projectClient = factory.GetProjectClient();

        // Assert
        projectClient.Should().NotBeNull();
        projectClient.Should().BeOfType<AIProjectClient>();
    }

    [TestMethod]
    public void GetProjectClient_WithApiKeyAuth_ShouldThrowInvalidOperationException()
    {
        // Arrange — API key auth does not support AIProjectClient
        var project = CreateMockProject(authType: AuthenticationType.ApiKey);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.GetProjectClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Entra ID*");
    }

    #endregion
}
