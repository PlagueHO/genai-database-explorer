using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;

namespace GenAIDBExplorer.Core.Test.SemanticKernel;

/// <summary>
/// Unit tests for <see cref="SemanticKernelFactory"/>.
/// </summary>
[TestClass]
public class SemanticKernelFactoryTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ILogger<SemanticKernelFactory>> _mockLogger = null!;
    private SemanticKernelFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Arrange
        _mockProject = new Mock<IProject>();
        _mockLogger = new Mock<ILogger<SemanticKernelFactory>>();
        _factory = new SemanticKernelFactory(_mockProject.Object, _mockLogger.Object);
    }

    [TestMethod]
    public void CreateSemanticKernel_WithAzureOpenAIConfiguration_ShouldCreateKernelSuccessfully()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        var result = _factory.CreateSemanticKernel();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Kernel>();
    }

    [TestMethod]
    public void CreateSemanticKernel_WithOpenAIConfiguration_ShouldCreateKernelSuccessfully()
    {
        // Arrange
        var settings = CreateOpenAISettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        var result = _factory.CreateSemanticKernel();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Kernel>();
    }

    [TestMethod]
    public void CreateSemanticKernel_ShouldLogDebugMessages()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        _factory.CreateSemanticKernel();

        // Assert
        VerifyLogWasCalled(LogLevel.Debug, "Creating Semantic Kernel instance");
        VerifyLogWasCalled(LogLevel.Debug, "Semantic Kernel instance created successfully");
    }

    [TestMethod]
    public void CreateSemanticKernel_ShouldLogServiceConfiguration()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        _factory.CreateSemanticKernel();

        // Assert
        VerifyLogWasCalled(LogLevel.Debug, "Adding ChatCompletion service");
        VerifyLogWasCalled(LogLevel.Debug, "Adding ChatCompletionStructured service");
        VerifyLogWasCalled(LogLevel.Debug, "Adding Azure OpenAI chat completion service");
        VerifyLogWasCalled(LogLevel.Debug, "Successfully added Azure OpenAI chat completion service");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithMissingAzureOpenAIDeploymentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        settings.OpenAIService.ChatCompletion.AzureOpenAIDeploymentId = null;
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateSemanticKernel());
        exception.Message.Should().Contain("AzureOpenAI deployment ID is required");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithMissingAzureOpenAIEndpoint_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        settings.OpenAIService.Default.AzureOpenAIEndpoint = null;
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateSemanticKernel());
        exception.Message.Should().Contain("AzureOpenAI endpoint is required");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithMissingAzureOpenAIApiKey_WhenUsingApiKeyAuth_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = CreateAzureOpenAISettings();
        settings.OpenAIService.Default.AzureAuthenticationType = AzureOpenAIAuthenticationType.ApiKey;
        settings.OpenAIService.Default.AzureOpenAIKey = null;
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateSemanticKernel());
        exception.Message.Should().Contain("AzureOpenAI API key is required");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithEntraIdAuthentication_ShouldSucceed()
    {
        // Arrange
        var settings = CreateAzureOpenAISettingsWithEntraId();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        var kernel = _factory.CreateSemanticKernel();

        // Assert
        kernel.Should().NotBeNull();
        VerifyLogWasCalled(LogLevel.Information, "Using Microsoft Entra ID Default authentication for Azure OpenAI (supports managed identity, Visual Studio, Azure CLI, etc.).");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithEntraIdAuthenticationAndTenantId_ShouldSucceed()
    {
        // Arrange
        var settings = CreateAzureOpenAISettingsWithEntraId();
        settings.OpenAIService.Default.TenantId = "12345678-1234-1234-1234-123456789012";
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        var kernel = _factory.CreateSemanticKernel();

        // Assert
        kernel.Should().NotBeNull();
        VerifyLogWasCalled(LogLevel.Information, "Using Microsoft Entra ID Default authentication for Azure OpenAI for tenant 12345678-1234-1234-1234-123456789012 (supports managed identity, Visual Studio, Azure CLI, etc.).");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithApiKeyAuthentication_ShouldSucceed()
    {
        // Arrange
        var settings = CreateAzureOpenAISettingsWithApiKey();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act
        var kernel = _factory.CreateSemanticKernel();

        // Assert
        kernel.Should().NotBeNull();
        VerifyLogWasCalled(LogLevel.Information, "Using API key authentication for Azure OpenAI.");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithMissingOpenAIModelId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = CreateOpenAISettings();
        settings.OpenAIService.ChatCompletion.ModelId = null;
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateSemanticKernel());
        exception.Message.Should().Contain("OpenAI model ID is required");
    }

    [TestMethod]
    public void CreateSemanticKernel_WithMissingOpenAIApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = CreateOpenAISettings();
        settings.OpenAIService.Default.OpenAIKey = null;
        _mockProject.Setup(p => p.Settings).Returns(settings);

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateSemanticKernel());
        exception.Message.Should().Contain("OpenAI API key is required");
    }

    private static ProjectSettings CreateAzureOpenAISettings()
    {
        return new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDatabase",
                ConnectionString = "Server=test;Database=TestDB;",
                Description = "Test database for unit tests"
            },
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration
                {
                    Directory = "SemanticModel"
                }
            },
            OpenAIService = new OpenAIServiceSettings
            {
                Default = new OpenAIServiceDefaultSettings
                {
                    ServiceType = "AzureOpenAI",
                    AzureOpenAIEndpoint = "https://test-aoai.openai.azure.com/",
                    AzureOpenAIKey = "test-api-key"
                },
                ChatCompletion = new OpenAIServiceChatCompletionSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o"
                },
                ChatCompletionStructured = new OpenAIServiceChatCompletionStructuredSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o-structured"
                }
            }
        };
    }

    private static ProjectSettings CreateAzureOpenAISettingsWithEntraId()
    {
        return new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDatabase",
                ConnectionString = "Server=test;Database=TestDB;",
                Description = "Test database for unit tests"
            },
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration
                {
                    Directory = "SemanticModel"
                }
            },
            OpenAIService = new OpenAIServiceSettings
            {
                Default = new OpenAIServiceDefaultSettings
                {
                    ServiceType = "AzureOpenAI",
                    AzureAuthenticationType = AzureOpenAIAuthenticationType.EntraIdAuthentication,
                    AzureOpenAIEndpoint = "https://test-aoai.openai.azure.com/"
                },
                ChatCompletion = new OpenAIServiceChatCompletionSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o"
                },
                ChatCompletionStructured = new OpenAIServiceChatCompletionStructuredSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o-structured"
                }
            }
        };
    }

    private static ProjectSettings CreateAzureOpenAISettingsWithApiKey()
    {
        return new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDatabase",
                ConnectionString = "Server=test;Database=TestDB;",
                Description = "Test database for unit tests"
            },
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration
                {
                    Directory = "SemanticModel"
                }
            },
            OpenAIService = new OpenAIServiceSettings
            {
                Default = new OpenAIServiceDefaultSettings
                {
                    ServiceType = "AzureOpenAI",
                    AzureAuthenticationType = AzureOpenAIAuthenticationType.ApiKey,
                    AzureOpenAIEndpoint = "https://test-aoai.openai.azure.com/",
                    AzureOpenAIKey = "test-api-key"
                },
                ChatCompletion = new OpenAIServiceChatCompletionSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o"
                },
                ChatCompletionStructured = new OpenAIServiceChatCompletionStructuredSettings
                {
                    AzureOpenAIDeploymentId = "gpt-4o-structured"
                }
            }
        };
    }

    private static ProjectSettings CreateOpenAISettings()
    {
        return new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDatabase",
                ConnectionString = "Server=test;Database=TestDB;",
                Description = "Test database for unit tests"
            },
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration
                {
                    Directory = "SemanticModel"
                }
            },
            OpenAIService = new OpenAIServiceSettings
            {
                Default = new OpenAIServiceDefaultSettings
                {
                    ServiceType = "OpenAI",
                    OpenAIKey = "test-api-key"
                },
                ChatCompletion = new OpenAIServiceChatCompletionSettings
                {
                    ModelId = "gpt-4o"
                },
                ChatCompletionStructured = new OpenAIServiceChatCompletionStructuredSettings
                {
                    ModelId = "gpt-4o"
                }
            }
        };
    }

    private void VerifyLogWasCalled(LogLevel logLevel, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
