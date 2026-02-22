using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Models.Project;

[TestClass]
public class ProjectSettingsIntegrationTests
{
    private Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>> _mockLogger = null!;
    private GenAIDBExplorer.Core.Models.Project.Project _project = null!;
    private DirectoryInfo _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>>();
        _project = new GenAIDBExplorer.Core.Models.Project.Project(_mockLogger.Object);

        // Create a unique test directory
        var tempPath = Path.Combine(Path.GetTempPath(), $"ProjectSettingsTests_{Guid.NewGuid():N}");
        _testDirectory = new DirectoryInfo(tempPath);
        Directory.CreateDirectory(_testDirectory.FullName);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_testDirectory?.Exists == true)
        {
            try
            {
                _testDirectory.Delete(true);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }

    [TestMethod]
    public void LoadProjectConfiguration_ValidSemanticModelRepositorySettings_ShouldBindCorrectly()
    {
        // Arrange
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "AzureBlob",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    },
                    "AzureBlob": {
                        "AccountEndpoint": "https://test.blob.core.windows.net",
                        "ContainerName": "test-models",
                        "BlobPrefix": "test",
                        "OperationTimeoutSeconds": 600,
                        "MaxConcurrentOperations": 8
                    },
                    "CosmosDb": {
                        "AccountEndpoint": "https://test.documents.azure.com:443/",
                        "DatabaseName": "TestSemanticModels",
                        "ModelsContainerName": "TestModels",
                        "EntitiesContainerName": "TestEntities",
                        "ConsistencyLevel": "Strong"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.cognitiveservices.azure.com/"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act
        _project.LoadProjectConfiguration(_testDirectory);

        // Assert
        var settings = _project.Settings;
        settings.Should().NotBeNull();

        // Verify SemanticModel settings
        settings.SemanticModel.Should().NotBeNull();
        settings.SemanticModel.PersistenceStrategy.Should().Be("AzureBlob");
        settings.SemanticModel.MaxDegreeOfParallelism.Should().Be(4);

        // Verify SemanticModelRepository settings
        settings.SemanticModelRepository.Should().NotBeNull();

        // Verify LocalDisk configuration
        settings.SemanticModelRepository.LocalDisk.Should().NotBeNull();
        settings.SemanticModelRepository.LocalDisk!.Directory.Should().Be("TestSemanticModel");

        // Verify AzureBlob configuration
        settings.SemanticModelRepository.AzureBlob.Should().NotBeNull();
        settings.SemanticModelRepository.AzureBlob!.AccountEndpoint.Should().Be("https://test.blob.core.windows.net");
        settings.SemanticModelRepository.AzureBlob.ContainerName.Should().Be("test-models");
        settings.SemanticModelRepository.AzureBlob.BlobPrefix.Should().Be("test");
        settings.SemanticModelRepository.AzureBlob.OperationTimeoutSeconds.Should().Be(600);
        settings.SemanticModelRepository.AzureBlob.MaxConcurrentOperations.Should().Be(8);

        // Verify CosmosDb configuration
        settings.SemanticModelRepository.CosmosDb.Should().NotBeNull();
        settings.SemanticModelRepository.CosmosDb!.AccountEndpoint.Should().Be("https://test.documents.azure.com:443/");
        settings.SemanticModelRepository.CosmosDb.DatabaseName.Should().Be("TestSemanticModels");
        settings.SemanticModelRepository.CosmosDb.ModelsContainerName.Should().Be("TestModels");
        settings.SemanticModelRepository.CosmosDb.EntitiesContainerName.Should().Be("TestEntities");
        settings.SemanticModelRepository.CosmosDb.ConsistencyLevel.Should().Be(CosmosConsistencyLevel.Strong);
    }

    [TestMethod]
    public void LoadProjectConfiguration_MissingAzureBlobConfigurationForAzureBlobStrategy_ShouldThrowValidationException()
    {
        // Arrange
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "AzureBlob",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.cognitiveservices.azure.com/"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act & Assert
        FluentActions.Invoking(() => _project.LoadProjectConfiguration(_testDirectory))
            .Should().Throw<ValidationException>()
            .WithMessage("*AzureBlob configuration is required when PersistenceStrategy is 'AzureBlob'*");
    }

    [TestMethod]
    public void LoadProjectConfiguration_InvalidPersistenceStrategy_ShouldThrowValidationException()
    {
        // Arrange
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "InvalidStrategy",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.cognitiveservices.azure.com/"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act & Assert
        FluentActions.Invoking(() => _project.LoadProjectConfiguration(_testDirectory))
            .Should().Throw<ValidationException>()
            .WithMessage("*Invalid PersistenceStrategy 'InvalidStrategy'*");
    }

    [TestMethod]
    public void LoadProjectConfiguration_OldOpenAIServiceSection_ShouldThrowValidationExceptionWithMigrationMessage()
    {
        // Arrange
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "LocalDisk",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.cognitiveservices.azure.com/"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                },
                "OpenAIService": {
                    "Default": {
                        "ServiceType": "AzureOpenAI"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act & Assert
        FluentActions.Invoking(() => _project.LoadProjectConfiguration(_testDirectory))
            .Should().Throw<ValidationException>()
            .WithMessage("*'OpenAIService'*has been replaced by 'FoundryModels'*");
    }

    [TestMethod]
    [DataRow("https://myresource.services.ai.azure.com/", DisplayName = "Foundry AI Services endpoint")]
    [DataRow("https://myresource.openai.azure.com/", DisplayName = "Azure OpenAI endpoint")]
    [DataRow("https://myresource.cognitiveservices.azure.com/", DisplayName = "Cognitive Services endpoint")]
    public void LoadProjectConfiguration_ValidEndpointPatterns_ShouldSucceed(string endpointUrl)
    {
        // Arrange
        var settingsJson = $$"""
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "LocalDisk",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "{{endpointUrl}}"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act & Assert - should not throw
        FluentActions.Invoking(() => _project.LoadProjectConfiguration(_testDirectory))
            .Should().NotThrow();
    }

    [TestMethod]
    public void LoadProjectConfiguration_InvalidEndpointDomain_ShouldThrowValidationException()
    {
        // Arrange
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": {
                    "Name": "TestDB",
                    "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                    "Description": "Test database"
                },
                "DataDictionary": {
                    "ColumnTypeMapping": []
                },
                "SemanticModel": {
                    "PersistenceStrategy": "LocalDisk",
                    "MaxDegreeOfParallelism": 4
                },
                "SemanticModelRepository": {
                    "LocalDisk": {
                        "Directory": "TestSemanticModel"
                    }
                },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://example.com/"
                    },
                    "ChatCompletion": {
                        "DeploymentName": "gpt-4o"
                    },
                    "ChatCompletionStructured": {
                        "DeploymentName": "gpt-4o"
                    },
                    "Embedding": {
                        "DeploymentName": "text-embedding-3-large"
                    }
                }
            }
            """;

        var settingsPath = Path.Combine(_testDirectory.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);

        // Act & Assert
        FluentActions.Invoking(() => _project.LoadProjectConfiguration(_testDirectory))
            .Should().Throw<ValidationException>()
            .WithMessage("*does not appear to be a valid Foundry Models endpoint*");
    }
}
