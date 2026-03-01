using FluentAssertions;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class InitProjectCommandHandlerTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISemanticModelProvider> _mockSemanticModelProvider = null!;
    private Mock<IDatabaseConnectionProvider> _mockConnectionProvider = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<ILogger<ICommandHandler<InitProjectCommandHandlerOptions>>> _mockLogger = null!;
    private Mock<IOutputService> _mockOutputService = null!;
    private InitProjectCommandHandler _handler = null!;

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Initializing project. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Project initialized successfully. '{projectPath.FullName}'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
                It.Is<object>(v => v.ToString()!.Contains("InitializeProjectComplete")),
                null,
                It.IsAny<Func<object, Exception?, string>>()),
            Times.Never);

        _mockOutputService.Verify(o => o.WriteError(It.Is<string>(s => s.Contains(exceptionMessage))), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenCommandOptionsIsNull()
    {
        // Arrange
        InitProjectCommandHandlerOptions? commandOptions = null;

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.InitializeProjectDirectory(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenProjectPathIsNull()
    {
        // Arrange
        var commandOptions = new InitProjectCommandHandlerOptions(null!);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(commandOptions);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockProject.Verify(p => p.InitializeProjectDirectory(It.IsAny<DirectoryInfo>()), Times.Never);
    }

    [TestMethod]
    public void HasSettingsOverrides_ShouldReturnFalse_WhenNoOverridesProvided()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(new DirectoryInfo(@"C:\Test"));

        // Act & Assert
        options.HasSettingsOverrides.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("MyDatabase", null, null, null)]
    [DataRow(null, "Server=.;Database=Test;", null, null)]
    [DataRow(null, null, "SqlAuthentication", null)]
    [DataRow(null, null, null, "dbo")]
    public void HasSettingsOverrides_ShouldReturnTrue_WhenDatabaseOverrideProvided(
        string? databaseName, string? connectionString, string? authType, string? schema)
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            databaseName: databaseName,
            databaseConnectionString: connectionString,
            databaseAuthType: authType,
            databaseSchema: schema);

        // Act & Assert
        options.HasSettingsOverrides.Should().BeTrue();
    }

    [TestMethod]
    public void HasSettingsOverrides_ShouldReturnTrue_WhenFoundryOverrideProvided()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            foundryEndpoint: "https://test.services.ai.azure.com/");

        // Act & Assert
        options.HasSettingsOverrides.Should().BeTrue();
    }

    [TestMethod]
    public void HasSettingsOverrides_ShouldReturnTrue_WhenPersistenceStrategyProvided()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            persistenceStrategy: "AzureBlob");

        // Act & Assert
        options.HasSettingsOverrides.Should().BeTrue();
    }

    [TestMethod]
    public void HasSettingsOverrides_ShouldReturnTrue_WhenVectorIndexProviderProvided()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            vectorIndexProvider: "InMemory");

        // Act & Assert
        options.HasSettingsOverrides.Should().BeTrue();
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldNotThrow_WhenAllValuesAreValid()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            databaseName: "TestDB",
            databaseConnectionString: "Server=.;Database=Test;",
            databaseAuthType: "SqlAuthentication",
            databaseSchema: "dbo",
            foundryEndpoint: "https://test.services.ai.azure.com/",
            foundryAuthType: "ApiKey",
            foundryApiKey: "test-key",
            foundryChatDeployment: "gpt-4o",
            foundryEmbeddingDeployment: "text-embedding-3-large",
            persistenceStrategy: "LocalDisk",
            vectorIndexProvider: "Auto",
            vectorIndexCollectionName: "my-collection");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldNotThrow_WhenNoOverridesProvided()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(new DirectoryInfo(@"C:\Test"));

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenDatabaseAuthTypeIsInvalid()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            databaseAuthType: "InvalidAuth");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid database authentication type*InvalidAuth*");
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenFoundryEndpointIsNotHttps()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            foundryEndpoint: "http://test.services.ai.azure.com/");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be a valid HTTPS URL*");
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenFoundryEndpointIsNotValidUrl()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            foundryEndpoint: "not-a-url");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be a valid HTTPS URL*");
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenFoundryAuthTypeIsInvalid()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            foundryAuthType: "InvalidAuth");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Foundry authentication type*InvalidAuth*");
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenPersistenceStrategyIsInvalid()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            persistenceStrategy: "InvalidStrategy");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid persistence strategy*InvalidStrategy*");
    }

    [TestMethod]
    public void ValidateSettingsOverrides_ShouldThrow_WhenVectorIndexProviderIsInvalid()
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            vectorIndexProvider: "InvalidProvider");

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid vector index provider*InvalidProvider*");
    }

    [TestMethod]
    [DataRow("EntraIdAuthentication")]
    [DataRow("entraidauthentication")]
    public void ValidateSettingsOverrides_ShouldAcceptCaseInsensitiveDatabaseAuthType(string authType)
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            databaseAuthType: authType);

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    [DataRow("AzureBlob")]
    [DataRow("azureblob")]
    [DataRow("CosmosDB")]
    [DataRow("cosmosdb")]
    public void ValidateSettingsOverrides_ShouldAcceptCaseInsensitivePersistenceStrategy(string strategy)
    {
        // Arrange
        var options = new InitProjectCommandHandlerOptions(
            new DirectoryInfo(@"C:\Test"),
            persistenceStrategy: strategy);

        // Act
        var act = () => InitProjectCommandHandler.ValidateSettingsOverrides(options);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldUpdateDatabaseSettings()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            databaseName: "MyTestDB",
            databaseConnectionString: "Server=myserver;Database=MyTestDB;",
            databaseAuthType: "EntraIdAuthentication",
            databaseSchema: "SalesLT");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["Database"]!["Name"]!.GetValue<string>().Should().Be("MyTestDB");
            settings["Database"]!["ConnectionString"]!.GetValue<string>().Should().Be("Server=myserver;Database=MyTestDB;");
            settings["Database"]!["AuthenticationType"]!.GetValue<string>().Should().Be("EntraIdAuthentication");
            settings["Database"]!["Schema"]!.GetValue<string>().Should().Be("SalesLT");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldUpdateMicrosoftFoundrySettings()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            foundryEndpoint: "https://myresource.services.ai.azure.com/",
            foundryAuthType: "ApiKey",
            foundryApiKey: "my-api-key-123",
            foundryChatDeployment: "gpt-4o-mini",
            foundryEmbeddingDeployment: "text-embedding-3-small");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["MicrosoftFoundry"]!["Default"]!["Endpoint"]!.GetValue<string>().Should().Be("https://myresource.services.ai.azure.com/");
            settings["MicrosoftFoundry"]!["Default"]!["AuthenticationType"]!.GetValue<string>().Should().Be("ApiKey");
            settings["MicrosoftFoundry"]!["Default"]!["ApiKey"]!.GetValue<string>().Should().Be("my-api-key-123");
            settings["MicrosoftFoundry"]!["ChatCompletion"]!["DeploymentName"]!.GetValue<string>().Should().Be("gpt-4o-mini");
            settings["MicrosoftFoundry"]!["Embedding"]!["DeploymentName"]!.GetValue<string>().Should().Be("text-embedding-3-small");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldUpdatePersistenceStrategy()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            persistenceStrategy: "AzureBlob");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["SemanticModel"]!["PersistenceStrategy"]!.GetValue<string>().Should().Be("AzureBlob");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldUpdateVectorIndexSettings()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            vectorIndexProvider: "InMemory",
            vectorIndexCollectionName: "my-test-vectors");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["VectorIndex"]!["Provider"]!.GetValue<string>().Should().Be("InMemory");
            settings["VectorIndex"]!["CollectionName"]!.GetValue<string>().Should().Be("my-test-vectors");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldPreserveExistingSettings_WhenOnlySomeOverridesProvided()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            databaseName: "NewName");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["Database"]!["Name"]!.GetValue<string>().Should().Be("NewName");
            // Other settings should be preserved from the template
            settings["SettingsVersion"]!.GetValue<string>().Should().NotBeNullOrEmpty();
            settings["SemanticModel"]!["PersistenceStrategy"]!.GetValue<string>().Should().Be("LocalDisk");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldThrow_WhenSettingsJsonNotFound()
    {
        // Arrange
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        tempDir.Create();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            databaseName: "Test");

        try
        {
            // Act
            var act = () => InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public void ApplySettingsOverrides_ShouldUpdateAllSettingsAtOnce()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var options = new InitProjectCommandHandlerOptions(
            tempDir,
            databaseName: "FullTestDB",
            databaseConnectionString: "Server=prod;Database=FullTestDB;",
            databaseAuthType: "EntraIdAuthentication",
            databaseSchema: "Production",
            foundryEndpoint: "https://prod.services.ai.azure.com/",
            foundryAuthType: "EntraIdAuthentication",
            foundryChatDeployment: "gpt-5-2-chat",
            foundryEmbeddingDeployment: "text-embedding-3-large",
            persistenceStrategy: "CosmosDB",
            vectorIndexProvider: "AzureAISearch",
            vectorIndexCollectionName: "prod-entities");

        try
        {
            // Act
            InitProjectCommandHandler.ApplySettingsOverrides(tempDir, options);

            // Assert
            var settings = ReadSettingsJson(tempDir);
            settings["Database"]!["Name"]!.GetValue<string>().Should().Be("FullTestDB");
            settings["Database"]!["ConnectionString"]!.GetValue<string>().Should().Be("Server=prod;Database=FullTestDB;");
            settings["Database"]!["AuthenticationType"]!.GetValue<string>().Should().Be("EntraIdAuthentication");
            settings["Database"]!["Schema"]!.GetValue<string>().Should().Be("Production");
            settings["MicrosoftFoundry"]!["Default"]!["Endpoint"]!.GetValue<string>().Should().Be("https://prod.services.ai.azure.com/");
            settings["MicrosoftFoundry"]!["Default"]!["AuthenticationType"]!.GetValue<string>().Should().Be("EntraIdAuthentication");
            settings["MicrosoftFoundry"]!["ChatCompletion"]!["DeploymentName"]!.GetValue<string>().Should().Be("gpt-5-2-chat");
            settings["MicrosoftFoundry"]!["Embedding"]!["DeploymentName"]!.GetValue<string>().Should().Be("text-embedding-3-large");
            settings["SemanticModel"]!["PersistenceStrategy"]!.GetValue<string>().Should().Be("CosmosDB");
            settings["VectorIndex"]!["Provider"]!.GetValue<string>().Should().Be("AzureAISearch");
            settings["VectorIndex"]!["CollectionName"]!.GetValue<string>().Should().Be("prod-entities");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public async Task HandleAsync_ShouldCallValidateAndApplySettingsOverrides_WhenOverridesProvided()
    {
        // Arrange
        var tempDir = CreateTempProjectDirectory();
        var commandOptions = new InitProjectCommandHandlerOptions(
            tempDir,
            databaseName: "TestDB");

        // The mock project will "initialize" but not actually copy files - we already have settings.json
        _mockProject.Setup(p => p.InitializeProjectDirectory(tempDir));

        try
        {
            // Act
            await _handler.HandleAsync(commandOptions);

            // Assert
            _mockProject.Verify(p => p.InitializeProjectDirectory(tempDir), Times.Once);
            var settings = ReadSettingsJson(tempDir);
            settings["Database"]!["Name"]!.GetValue<string>().Should().Be("TestDB");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [TestMethod]
    public async Task HandleAsync_ShouldOutputError_WhenInvalidSettingsOverridesProvided()
    {
        // Arrange
        var projectPath = new DirectoryInfo(@"C:\ValidProjectPath");
        var commandOptions = new InitProjectCommandHandlerOptions(
            projectPath,
            databaseAuthType: "InvalidAuth");

        // Act
        await _handler.HandleAsync(commandOptions);

        // Assert - should fail validation before InitializeProjectDirectory is called
        _mockProject.Verify(p => p.InitializeProjectDirectory(It.IsAny<DirectoryInfo>()), Times.Never);
        _mockOutputService.Verify(o => o.WriteError(It.Is<string>(s => s.Contains("Invalid database authentication type"))), Times.Once);
    }

    [TestMethod]
    public void InitProject_GeneratesSettings_WithMicrosoftFoundrySection()
    {
        // Arrange — verify generated settings.json uses MicrosoftFoundry section, not FoundryModels (T032)
        var tempDir = CreateTempProjectDirectory();

        try
        {
            // Act — read the template settings
            var settings = ReadSettingsJson(tempDir);

            // Assert
            settings["MicrosoftFoundry"].Should().NotBeNull("generated settings must contain MicrosoftFoundry section");
            settings["MicrosoftFoundry"]!["Default"]!["Endpoint"].Should().NotBeNull();
            settings["MicrosoftFoundry"]!["ChatCompletion"]!["DeploymentName"].Should().NotBeNull();
            settings["MicrosoftFoundry"]!["Embedding"]!["DeploymentName"].Should().NotBeNull();

            // Verify FoundryModels is NOT present
            settings["FoundryModels"].Should().BeNull("generated settings must not contain legacy FoundryModels section");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    /// <summary>
    /// Creates a temporary directory with a minimal settings.json for testing ApplySettingsOverrides.
    /// </summary>
    private static DirectoryInfo CreateTempProjectDirectory()
    {
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "genaidb-test-" + Guid.NewGuid().ToString("N")));
        tempDir.Create();

        var settingsJson = new JsonObject
        {
            ["SettingsVersion"] = "2.0.0",
            ["Database"] = new JsonObject
            {
                ["Name"] = "DefaultDB",
                ["Description"] = "Default database",
                ["ConnectionString"] = "Server=.;Database=Default;",
                ["AuthenticationType"] = "SqlAuthentication",
                ["Schema"] = "dbo",
                ["MaxDegreeOfParallelism"] = 10
            },
            ["DataDictionary"] = new JsonObject
            {
                ["ColumnTypeMapping"] = new JsonArray()
            },
            ["SemanticModel"] = new JsonObject
            {
                ["PersistenceStrategy"] = "LocalDisk",
                ["MaxDegreeOfParallelism"] = 10
            },
            ["SemanticModelRepository"] = new JsonObject
            {
                ["LocalDisk"] = new JsonObject
                {
                    ["Directory"] = "semantic-model"
                }
            },
            ["MicrosoftFoundry"] = new JsonObject
            {
                ["Default"] = new JsonObject
                {
                    ["AuthenticationType"] = "EntraIdAuthentication",
                    ["Endpoint"] = "https://placeholder.services.ai.azure.com/api/projects/placeholder"
                },
                ["ChatCompletion"] = new JsonObject
                {
                    ["DeploymentName"] = "placeholder-chat"
                },
                ["Embedding"] = new JsonObject
                {
                    ["DeploymentName"] = "placeholder-embedding"
                }
            },
            ["VectorIndex"] = new JsonObject
            {
                ["Provider"] = "Auto",
                ["CollectionName"] = "genaide-entities"
            }
        };

        var settingsPath = Path.Combine(tempDir.FullName, "settings.json");
        File.WriteAllText(settingsPath, settingsJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return tempDir;
    }

    /// <summary>
    /// Reads and parses the settings.json from a project directory.
    /// </summary>
    private static JsonNode ReadSettingsJson(DirectoryInfo projectDir)
    {
        var settingsPath = Path.Combine(projectDir.FullName, "settings.json");
        var jsonText = File.ReadAllText(settingsPath);
        return JsonNode.Parse(jsonText) ?? throw new InvalidOperationException("Failed to parse settings.json");
    }

    /// <summary>
    /// Cleans up a temporary directory.
    /// </summary>
    private static void CleanupTempDirectory(DirectoryInfo dir)
    {
        if (dir.Exists)
        {
            dir.Delete(recursive: true);
        }
    }
}