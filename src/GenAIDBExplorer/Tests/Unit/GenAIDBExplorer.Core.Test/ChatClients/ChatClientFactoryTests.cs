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
        string serviceType = "AzureOpenAI",
        AzureOpenAIAuthenticationType authType = AzureOpenAIAuthenticationType.ApiKey,
        string? endpoint = "https://test.openai.azure.com/",
        string? apiKey = "test-api-key",
        string? chatDeploymentId = "gpt-4o",
        string? chatStructuredDeploymentId = "gpt-4o-structured",
        string? embeddingDeploymentId = "text-embedding-3-small",
        string? tenantId = null)
    {
        var projectMock = new Mock<IProject>();
        var settings = new ProjectSettings
        {
            SettingsVersion = new Version(1, 0),
            Database = new DatabaseSettings(),
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            OpenAIService = new OpenAIServiceSettings
            {
                Default = new OpenAIServiceDefaultSettings
                {
                    ServiceType = serviceType,
                    AzureAuthenticationType = authType,
                    AzureOpenAIEndpoint = endpoint,
                    AzureOpenAIKey = apiKey,
                    TenantId = tenantId
                },
                ChatCompletion = new OpenAIServiceChatCompletionSettings
                {
                    AzureOpenAIDeploymentId = chatDeploymentId
                },
                ChatCompletionStructured = new OpenAIServiceChatCompletionStructuredSettings
                {
                    AzureOpenAIDeploymentId = chatStructuredDeploymentId
                },
                Embedding = new OpenAIServiceEmbeddingSettings
                {
                    AzureOpenAIDeploymentId = embeddingDeploymentId
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
        var project = CreateMockProject(authType: AzureOpenAIAuthenticationType.ApiKey);
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
        var project = CreateMockProject(authType: AzureOpenAIAuthenticationType.EntraIdAuthentication);
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
        var project = CreateMockProject(chatDeploymentId: null);
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
            authType: AzureOpenAIAuthenticationType.ApiKey,
            apiKey: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateChatClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    #endregion

    #region CreateStructuredOutputChatClient

    [TestMethod]
    public void CreateStructuredOutputChatClient_WithApiKeyAuth_ShouldReturnIChatClient()
    {
        // Arrange
        var project = CreateMockProject(authType: AzureOpenAIAuthenticationType.ApiKey);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var client = factory.CreateStructuredOutputChatClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IChatClient>();
    }

    [TestMethod]
    public void CreateStructuredOutputChatClient_MissingDeploymentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var project = CreateMockProject(chatStructuredDeploymentId: null);
        var logger = Mock.Of<ILogger<ChatClientFactory>>();
        var factory = new ChatClientFactory(project, logger);

        // Act
        var act = () => factory.CreateStructuredOutputChatClient();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment*");
    }

    #endregion

    #region CreateEmbeddingGenerator

    [TestMethod]
    public void CreateEmbeddingGenerator_WithApiKeyAuth_ShouldReturnEmbeddingGenerator()
    {
        // Arrange
        var project = CreateMockProject(authType: AzureOpenAIAuthenticationType.ApiKey);
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
        var project = CreateMockProject(authType: AzureOpenAIAuthenticationType.EntraIdAuthentication);
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
        var project = CreateMockProject(embeddingDeploymentId: null);
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
            authType: AzureOpenAIAuthenticationType.EntraIdAuthentication,
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
}
