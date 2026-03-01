using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.Test.Models.Project;

[TestClass]
public class ProjectTests
{
    private Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>> _loggerMock = null!;
    private string _testRoot = null!;
    private string _defaultProjectPath = null!;

    // Static lock to prevent tests from interfering with each other
    private static readonly object _defaultProjectLock = new object();

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>>();
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
        _defaultProjectPath = Path.Combine(_testRoot, "DefaultProject");
        Directory.CreateDirectory(_defaultProjectPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, true);
    }

    [TestMethod]
    public void InitializeProjectDirectory_CreatesDirectoryIfNotExists()
    {
        lock (_defaultProjectLock)
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NewProject"));
            var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

            // This test uses the existing DefaultProject in the app directory
            // We just need to ensure it exists for the test to pass
            var appBaseDir = AppContext.BaseDirectory;
            var defaultProjectPath = Path.Combine(appBaseDir, "DefaultProject");

            // Create a minimal DefaultProject if it doesn't exist
            var needsCleanup = false;
            if (!Directory.Exists(defaultProjectPath))
            {
                needsCleanup = true;
                Directory.CreateDirectory(defaultProjectPath);

                var genaiDbExplorerDir = Path.Combine(defaultProjectPath, ".genaidbexplorer");
                Directory.CreateDirectory(genaiDbExplorerDir);

                // Create minimal settings.json
                var settingsContent = """
                {
                  "ConnectionString": "",
                  "DatabaseName": "TestDatabase",
                  "OutputFormats": ["json", "markdown"],
                  "MaxRetries": 3,
                  "RetryDelay": "00:00:05"
                }
                """;
                File.WriteAllText(Path.Combine(defaultProjectPath, "settings.json"), settingsContent);

                // Create version.json
                var versionContent = """
                {
                  "version": "1.0.0",
                  "created": "2024-01-01T00:00:00Z"
                }
                """;
                File.WriteAllText(Path.Combine(genaiDbExplorerDir, "version.json"), versionContent);
            }

            try
            {
                // Act
                project.InitializeProjectDirectory(projectDir);

                // Assert
                projectDir.Exists.Should().BeTrue();
                File.Exists(Path.Combine(projectDir.FullName, "settings.json")).Should().BeTrue();
                Directory.Exists(Path.Combine(projectDir.FullName, ".genaidbexplorer")).Should().BeTrue();
                File.Exists(Path.Combine(projectDir.FullName, ".genaidbexplorer", "version.json")).Should().BeTrue();
            }
            finally
            {
                // Clean up the temporary DefaultProject if we created it
                if (needsCleanup && Directory.Exists(defaultProjectPath))
                {
                    try
                    {
                        Directory.Delete(defaultProjectPath, true);
                    }
                    catch (IOException)
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }

    [TestMethod]
    public void InitializeProjectDirectory_ThrowsIfDirectoryNotEmpty()
    {
        // Arrange
        var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NotEmptyProject"));
        projectDir.Create();
        File.WriteAllText(Path.Combine(projectDir.FullName, "file.txt"), "data");
        var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

        // This test doesn't need DefaultProject to exist to test the directory not empty condition
        // The method should check if the directory is empty before trying to access DefaultProject

        // Act
        Action act = () => project.InitializeProjectDirectory(projectDir);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void InitializeProjectDirectory_ThrowsIfDefaultProjectMissing()
    {
        lock (_defaultProjectLock)
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "MissingDefaultProject"));
            var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

            // This test verifies that the method properly handles the case where DefaultProject is missing
            var originalBaseDirectory = AppContext.BaseDirectory;
            var defaultProjectPath = Path.Combine(originalBaseDirectory, "DefaultProject");
            var defaultProjectBackupPath = defaultProjectPath + "_test_backup_" + Guid.NewGuid().ToString("N")[..8];

            // Check if DefaultProject exists and back it up temporarily
            bool defaultProjectExisted = Directory.Exists(defaultProjectPath);

            try
            {
                if (defaultProjectExisted)
                {
                    // Move the DefaultProject temporarily to simulate it being missing
                    Directory.Move(defaultProjectPath, defaultProjectBackupPath);
                }

                // Verify DefaultProject doesn't exist
                Directory.Exists(defaultProjectPath).Should().BeFalse("DefaultProject should not exist for this test");

                // Act & Assert
                Action act = () => project.InitializeProjectDirectory(projectDir);
                act.Should().ThrowExactly<DirectoryNotFoundException>()
                    .WithMessage("*DefaultProject directory not found*");
            }
            finally
            {
                // Restore the DefaultProject if it existed before
                if (defaultProjectExisted && Directory.Exists(defaultProjectBackupPath))
                {
                    try
                    {
                        if (Directory.Exists(defaultProjectPath))
                        {
                            Directory.Delete(defaultProjectPath, true);
                        }
                        Directory.Move(defaultProjectBackupPath, defaultProjectPath);
                    }
                    catch (IOException)
                    {
                        // If restore fails, try to clean up the backup at least
                        try { Directory.Delete(defaultProjectBackupPath, true); } catch { }
                    }
                }
            }
        }
    }

    #region Settings Validation Tests (T008-T014)

    private GenAIDBExplorer.Core.Models.Project.Project CreateProjectForValidation()
    {
        return new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);
    }

    private string CreateSettingsFile(string settingsJson)
    {
        var dir = Path.Combine(_testRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "settings.json"), settingsJson);
        return dir;
    }

    private static string ValidMicrosoftFoundrySettingsJson(
        string settingsVersion = "2.0.0",
        string endpoint = "https://test.services.ai.azure.com/api/projects/testproject") => $$"""
        {
            "SettingsVersion": "{{settingsVersion}}",
            "Database": {
                "Name": "TestDB",
                "ConnectionString": "Server=.;Database=Test;Integrated Security=true;",
                "Description": "Test database"
            },
            "DataDictionary": { "ColumnTypeMapping": [] },
            "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
            "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
            "MicrosoftFoundry": {
                "Default": {
                    "AuthenticationType": "ApiKey",
                    "ApiKey": "test-key",
                    "Endpoint": "{{endpoint}}"
                },
                "ChatCompletion": { "DeploymentName": "gpt-4o" },
                "Embedding": { "DeploymentName": "text-embedding-3-large" }
            }
        }
        """;

    [TestMethod]
    public void ValidateSettings_LegacyFoundryModelsSection_ReturnsActionableError()
    {
        // T008: Settings with FoundryModels section only should return actionable error
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.openai.azure.com/"
                    },
                    "ChatCompletion": { "DeploymentName": "gpt-4o" },
                    "Embedding": { "DeploymentName": "text-embedding-3-large" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*'FoundryModels'*renamed*'MicrosoftFoundry'*");
    }

    [TestMethod]
    public void ValidateSettings_SettingsVersionBelowTwo_ReturnsActionableError()
    {
        // T009: Settings with SettingsVersion 1.0.0 should return actionable error
        var settingsJson = ValidMicrosoftFoundrySettingsJson(settingsVersion: "1.0.0");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*version*no longer supported*2.0.0*");
    }

    [TestMethod]
    public void ValidateSettings_BothFoundryModelsAndMicrosoftFoundry_ReturnsAmbiguityError()
    {
        // T010: Both sections present should return ambiguity error
        var settingsJson = """
            {
                "SettingsVersion": "2.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "MicrosoftFoundry": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.services.ai.azure.com/api/projects/testproject"
                    },
                    "ChatCompletion": { "DeploymentName": "gpt-4o" },
                    "Embedding": { "DeploymentName": "text-embedding-3-large" }
                },
                "FoundryModels": {
                    "Default": { "Endpoint": "https://old.openai.azure.com/" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*Both*'FoundryModels'*'MicrosoftFoundry'*Remove*legacy*");
    }

    [TestMethod]
    public void ValidateSettings_OnlyMicrosoftFoundrySection_NoLegacyErrors()
    {
        // T011: Valid v2.0.0 settings with only MicrosoftFoundry should have no legacy warnings
        var settingsJson = ValidMicrosoftFoundrySettingsJson();

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().NotThrow();
    }

    [TestMethod]
    public void ValidateSettings_LegacyOpenAIServiceSection_ReturnsError()
    {
        // T012: Existing OpenAIService legacy detection still works
        var settingsJson = """
            {
                "SettingsVersion": "2.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "MicrosoftFoundry": {
                    "Default": { "AuthenticationType": "ApiKey" }
                },
                "OpenAIService": {
                    "Default": { "ServiceType": "AzureOpenAI" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*'OpenAIService'*replaced by 'MicrosoftFoundry'*");
    }

    [TestMethod]
    public void ValidateEndpoint_ValidProjectEndpoint_Passes()
    {
        // T013: Valid project endpoint passes validation
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "https://myresource.services.ai.azure.com/api/projects/myproject");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().NotThrow();
    }

    [TestMethod]
    public void ValidateEndpoint_ResourceOnlyEndpoint_RejectsWithError()
    {
        // T013: Resource-only endpoint (no project path) is rejected
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "https://myresource.services.ai.azure.com/");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*must be a Microsoft Foundry project endpoint*'/api/projects/*");
    }

    [TestMethod]
    public void ValidateEndpoint_LegacyOpenAIEndpoint_RejectsWithError()
    {
        // T013: Legacy *.openai.azure.com endpoint is rejected
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "https://myresource.openai.azure.com/");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*Legacy Azure OpenAI endpoints*not supported*");
    }

    [TestMethod]
    public void ValidateEndpoint_LegacyCognitiveServicesEndpoint_RejectsWithError()
    {
        // T013: Legacy *.cognitiveservices.azure.com endpoint is rejected
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "https://myresource.cognitiveservices.azure.com/");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*Legacy Cognitive Services endpoints*not supported*");
    }

    [TestMethod]
    public void ValidateEndpoint_ProjectEndpointWithTrailingSlash_Passes()
    {
        // T013: Trailing slash on project endpoint is accepted
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "https://myresource.services.ai.azure.com/api/projects/myproject/");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().NotThrow();
    }

    [TestMethod]
    public void ValidateEndpoint_NonHttpsEndpoint_RejectsWithError()
    {
        // T013: Non-HTTPS endpoint is rejected
        var settingsJson = ValidMicrosoftFoundrySettingsJson(
            endpoint: "http://myresource.services.ai.azure.com/api/projects/myproject");

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*must use HTTPS*");
    }

    [TestMethod]
    public void ValidateEndpoint_MissingDeployment_ReturnsClearError()
    {
        // T014: Missing deployment name produces clear error
        var settingsJson = """
            {
                "SettingsVersion": "2.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "MicrosoftFoundry": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.services.ai.azure.com/api/projects/testproject"
                    },
                    "ChatCompletion": { },
                    "Embedding": { "DeploymentName": "text-embedding-3-large" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*ChatCompletion.DeploymentName*required*");
    }

    #endregion

    #region Legacy Error Message Tests (Phase 5)

    [TestMethod]
    public void ValidateSettings_LegacyFoundryModelsSection_ErrorIncludesBeforeAfterExample()
    {
        // T039: Verify error message includes concrete before/after JSON snippet
        var settingsJson = """
            {
                "SettingsVersion": "1.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "FoundryModels": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://test.services.ai.azure.com/"
                    },
                    "ChatCompletion": { "DeploymentName": "gpt-4o" },
                    "Embedding": { "DeploymentName": "text-embedding-3-large" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*\"FoundryModels\"*")
            .WithMessage("*\"MicrosoftFoundry\"*")
            .WithMessage("*Before:*")
            .WithMessage("*After:*")
            .WithMessage("*2.0.0*");
    }

    [TestMethod]
    public void ValidateSettings_LegacyEndpointInMicrosoftFoundrySection_ReturnsSpecificError()
    {
        // T040: MicrosoftFoundry section with legacy *.openai.azure.com endpoint
        var settingsJson = """
            {
                "SettingsVersion": "2.0.0",
                "Database": { "Name": "TestDB", "ConnectionString": "Server=.;Database=Test;Integrated Security=true;", "Description": "Test" },
                "DataDictionary": { "ColumnTypeMapping": [] },
                "SemanticModel": { "PersistenceStrategy": "LocalDisk", "MaxDegreeOfParallelism": 4 },
                "SemanticModelRepository": { "LocalDisk": { "Directory": "TestSemanticModel" } },
                "MicrosoftFoundry": {
                    "Default": {
                        "AuthenticationType": "ApiKey",
                        "ApiKey": "test-key",
                        "Endpoint": "https://myresource.openai.azure.com/"
                    },
                    "ChatCompletion": { "DeploymentName": "gpt-4o" },
                    "Embedding": { "DeploymentName": "text-embedding-3-large" }
                }
            }
            """;

        var dir = CreateSettingsFile(settingsJson);
        var project = CreateProjectForValidation();

        FluentActions.Invoking(() => project.LoadProjectConfiguration(new DirectoryInfo(dir)))
            .Should().Throw<ValidationException>()
            .WithMessage("*Legacy Azure OpenAI endpoints*not supported*")
            .WithMessage("*Foundry project endpoint*");
    }

    #endregion
}
