using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Resources;

namespace GenAIDBExplorer.Core.Models.Project;

public class Project(
    ILogger<Project> logger
) : IProject
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(Project).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(Project).Assembly);

    /// <summary>
    /// Configuration instance for accessing project settings.
    /// </summary>
    private IConfiguration? _configuration;

    /// <summary>
    /// Gets the project settings.
    /// </summary>
    public ProjectSettings Settings { get; private set; } = null!;

    /// <summary>
    /// Gets the project directory.
    /// </summary>
    public DirectoryInfo ProjectDirectory { get; private set; } = null!;

    /// <summary>
    /// Initializes the project directory by copying the default project structure.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    public void InitializeProjectDirectory(DirectoryInfo projectDirectory)
    {
        ProjectDirectory = projectDirectory;

        // Create the directory if it does not exist
        if (!projectDirectory.Exists)
        {
            logger.LogInformation("Project directory does not exist. Creating: {ProjectDirectory}", projectDirectory.FullName);
            projectDirectory.Create();
        }

        if (ProjectUtils.IsDirectoryNotEmpty(projectDirectory))
        {
            logger.LogError("{ErrorMessage}", _resourceManagerErrorMessages.GetString("ErrorProjectFolderNotEmpty"));
            // Throw exception directory is not empty
            throw new InvalidOperationException(_resourceManagerErrorMessages.GetString("ErrorProjectFolderNotEmpty"));
        }

        var defaultProjectPath = Path.Combine(AppContext.BaseDirectory, "DefaultProject");
        var defaultProjectDirectory = new DirectoryInfo(defaultProjectPath);

        logger.LogDebug("Looking for DefaultProject directory at: {DefaultProjectPath}", defaultProjectPath);

        if (!defaultProjectDirectory.Exists)
        {
            var errorMessage = $"DefaultProject directory not found at: {defaultProjectPath}";
            logger.LogError("{ErrorMessage}", errorMessage);
            throw new DirectoryNotFoundException(errorMessage);
        }

        logger.LogDebug("Copying DefaultProject from {SourcePath} to {DestinationPath}", defaultProjectPath, projectDirectory.FullName);
        ProjectUtils.CopyDirectory(defaultProjectDirectory, projectDirectory);
    }

    /// <inheritdoc />
    public DirectoryInfo GetSemanticModelPath()
    {
        var strategy = Settings.SemanticModel?.PersistenceStrategy?.ToLowerInvariant() ?? "localdisk";
        return strategy switch
        {
            "localdisk" => new DirectoryInfo(
                Path.Combine(
                    ProjectDirectory.FullName,
                    Settings.SemanticModelRepository?.LocalDisk?.Directory
                        ?? throw new InvalidOperationException(
                            "LocalDisk persistence strategy is configured but no directory is specified in SemanticModelRepository.LocalDisk.Directory."))),
            _ => ProjectDirectory
        };
    }

    /// <summary>
    /// Loads the configuration from the specified project path.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to load the configuration from.</param>
    public void LoadProjectConfiguration(DirectoryInfo projectDirectory)
    {
        ProjectDirectory = projectDirectory;

        // Create IConfiguration from the projectPath
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectDirectory.FullName)
            .AddJsonFile("settings.json", optional: false, reloadOnChange: false);

        _configuration = configurationBuilder.Build();

        InitializeSettings();
    }

    /// <summary>
    /// Initializes the project settings and binds configuration sections.
    /// </summary>
    private void InitializeSettings()
    {
        // Initialize ProjectSettings and bind configuration sections
        Settings = new ProjectSettings
        {
            Database = new DatabaseSettings(),
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration?.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration?.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration?.GetSection(DataDictionarySettings.PropertyName).Bind(Settings.DataDictionary);
        _configuration?.GetSection(SemanticModelSettings.PropertyName).Bind(Settings.SemanticModel);
        _configuration?.GetSection(MicrosoftFoundrySettings.PropertyName).Bind(Settings.MicrosoftFoundry);
        _configuration?.GetSection(SemanticModelRepositorySettings.PropertyName).Bind(Settings.SemanticModelRepository);
        _configuration?.GetSection(VectorIndexSettings.PropertyName).Bind(Settings.VectorIndex);

        ValidateSettings();
    }

    /// <summary>
    /// Validates the project settings.
    /// </summary>
    private void ValidateSettings()
    {
        logger.LogInformation("{Message}", _resourceManagerLogMessages.GetString("ProjectSettingsValidationStarted"));

        // Detect legacy FoundryModels section (FR-009)
        var legacyFoundryModelsSection = _configuration?.GetSection("FoundryModels");
        var legacyFoundryModelsExists = legacyFoundryModelsSection?.GetChildren()?.Any() == true;
        var microsoftFoundrySection = _configuration?.GetSection("MicrosoftFoundry");
        var microsoftFoundryExists = microsoftFoundrySection?.GetChildren()?.Any() == true;

        // Check for dual-section ambiguity (T018)
        if (legacyFoundryModelsExists && microsoftFoundryExists)
        {
            throw new ValidationException(
                "Both 'FoundryModels' and 'MicrosoftFoundry' sections found in settings.json. " +
                "Remove the legacy 'FoundryModels' section and keep only 'MicrosoftFoundry'.\n\n" +
                "Migration: Delete the entire \"FoundryModels\" block from your settings.json.");
        }

        // Check for legacy FoundryModels section (T016)
        if (legacyFoundryModelsExists)
        {
            throw new ValidationException(
                "The 'FoundryModels' configuration section has been renamed to 'MicrosoftFoundry'. " +
                "Please rename it in your settings.json and update the endpoint to a Foundry project endpoint format.\n\n" +
                "Before:\n" +
                "  \"FoundryModels\": {\n" +
                "    \"Default\": { \"Endpoint\": \"https://<resource>.services.ai.azure.com/\" }\n" +
                "  }\n\n" +
                "After:\n" +
                "  \"SettingsVersion\": \"2.0.0\",\n" +
                "  \"MicrosoftFoundry\": {\n" +
                "    \"Default\": { \"Endpoint\": \"https://<resource>.services.ai.azure.com/api/projects/<project-name>\" }\n" +
                "  }");
        }

        // Check SettingsVersion (T017, FR-021)
        if (Settings.SettingsVersion is not null && Settings.SettingsVersion < new Version(2, 0))
        {
            throw new ValidationException(
                $"Settings version {Settings.SettingsVersion} is no longer supported. Version 2.0.0 is required. " +
                "The 'FoundryModels' section has been renamed to 'MicrosoftFoundry' and the endpoint must be " +
                "a Foundry project endpoint.\n\n" +
                "Migration steps:\n" +
                "1. Rename \"FoundryModels\" to \"MicrosoftFoundry\"\n" +
                "2. Update endpoint to: https://<resource>.services.ai.azure.com/api/projects/<project-name>\n" +
                "3. Set \"SettingsVersion\": \"2.0.0\"");
        }

        var validationContext = new ValidationContext(Settings.Database);
        Validator.ValidateObject(Settings.Database, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "Database");

        validationContext = new ValidationContext(Settings.DataDictionary);
        Validator.ValidateObject(Settings.DataDictionary, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "DataDictionary");

        validationContext = new ValidationContext(Settings.SemanticModel);
        Validator.ValidateObject(Settings.SemanticModel, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "SemanticModel");

        // Validate persistence strategy-specific configuration
        ValidatePersistenceStrategyConfiguration();

        validationContext = new ValidationContext(Settings.SemanticModelRepository);
        Validator.ValidateObject(Settings.SemanticModelRepository, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "SemanticModelRepository");

        // Validate VectorIndex settings
        var vectorIndexContext = new ValidationContext(Settings.VectorIndex);
        Validator.ValidateObject(Settings.VectorIndex, vectorIndexContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "VectorIndex");

        validationContext = new ValidationContext(Settings.MicrosoftFoundry);
        Validator.ValidateObject(Settings.MicrosoftFoundry, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "MicrosoftFoundry");

        // Validate Microsoft Foundry-specific configuration
        ValidateMicrosoftFoundryConfiguration();

        logger.LogInformation("{Message}", _resourceManagerLogMessages.GetString("ProjectSettingsValidationCompleted"));
    }

    /// <summary>
    /// Validates that the appropriate configuration exists for the selected persistence strategy.
    /// </summary>
    private void ValidatePersistenceStrategyConfiguration()
    {
        var strategy = Settings.SemanticModel.PersistenceStrategy;

        switch (strategy?.ToLowerInvariant())
        {
            case "localdisk":
                if (Settings.SemanticModelRepository.LocalDisk == null)
                {
                    throw new ValidationException("LocalDisk configuration is required when PersistenceStrategy is 'LocalDisk'.");
                }
                var localDiskContext = new ValidationContext(Settings.SemanticModelRepository.LocalDisk);
                Validator.ValidateObject(Settings.SemanticModelRepository.LocalDisk, localDiskContext, validateAllProperties: true);
                break;

            case "azureblob":
                if (Settings.SemanticModelRepository.AzureBlob == null)
                {
                    throw new ValidationException("AzureBlob configuration is required when PersistenceStrategy is 'AzureBlob'.");
                }
                var azureBlobContext = new ValidationContext(Settings.SemanticModelRepository.AzureBlob);
                Validator.ValidateObject(Settings.SemanticModelRepository.AzureBlob, azureBlobContext, validateAllProperties: true);
                break;

            case "cosmosdb":
                if (Settings.SemanticModelRepository.CosmosDb == null)
                {
                    throw new ValidationException("CosmosDb configuration is required when PersistenceStrategy is 'CosmosDb'.");
                }
                var cosmosContext = new ValidationContext(Settings.SemanticModelRepository.CosmosDb);
                Validator.ValidateObject(Settings.SemanticModelRepository.CosmosDb, cosmosContext, validateAllProperties: true);
                break;

            default:
                throw new ValidationException($"Invalid PersistenceStrategy '{strategy}'. Valid values are: LocalDisk, AzureBlob, Cosmos.");
        }
    }

    /// <summary>
    /// Validates Microsoft Foundry configuration to ensure endpoints are valid URIs and required settings are present.
    /// </summary>
    private void ValidateMicrosoftFoundryConfiguration()
    {
        // Detect old OpenAIService section and provide migration guidance.
        // Use GetChildren().Any() to check for actual child configuration keys,
        // avoiding false positives from IConfigurationSection.Exists() when the section
        // has no real content.
        var openAiSection = _configuration?.GetSection("OpenAIService");
        if (openAiSection?.GetChildren()?.Any() == true)
        {
            // If MicrosoftFoundry is already properly configured, log a warning instead of throwing.
            // This allows partial migration scenarios where both sections exist.
            if (Settings.MicrosoftFoundry?.Default?.Endpoint is not null)
            {
                logger.LogWarning(
                    "Found legacy 'OpenAIService' configuration section alongside 'MicrosoftFoundry'. " +
                    "Using 'MicrosoftFoundry' configuration. Please remove the 'OpenAIService' section from your settings.json.");
            }
            else
            {
                throw new ValidationException(
                    "The 'OpenAIService' configuration section has been replaced by 'MicrosoftFoundry'. " +
                    "Please update your settings.json file. See documentation for the new configuration format.");
            }
        }

        var foundrySettings = Settings.MicrosoftFoundry?.Default;
        if (foundrySettings == null)
        {
            return; // Basic validation will catch this
        }

        // Validate Endpoint URL format
        if (!string.IsNullOrEmpty(foundrySettings.Endpoint))
        {
            if (!Uri.TryCreate(foundrySettings.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new ValidationException(
                    $"Endpoint '{foundrySettings.Endpoint}' is not a valid URL. " +
                    "Expected format: https://<resource>.services.ai.azure.com/api/projects/<project-name>");
            }

            // Validate that it's an HTTPS URL
            if (endpointUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ValidationException(
                    $"Endpoint must use HTTPS. Current URL: {foundrySettings.Endpoint}");
            }

            // Reject legacy *.openai.azure.com endpoints
            var host = endpointUri.Host.ToLowerInvariant();
            if (host.EndsWith(".openai.azure.com"))
            {
                throw new ValidationException(
                    $"Legacy Azure OpenAI endpoints (*.openai.azure.com) are not supported. " +
                    $"Use a Microsoft Foundry project endpoint instead: " +
                    $"https://<resource>.services.ai.azure.com/api/projects/<project-name>");
            }

            // Reject legacy *.cognitiveservices.azure.com endpoints
            if (host.EndsWith(".cognitiveservices.azure.com"))
            {
                throw new ValidationException(
                    $"Legacy Cognitive Services endpoints (*.cognitiveservices.azure.com) are not supported. " +
                    $"Use a Microsoft Foundry project endpoint instead: " +
                    $"https://<resource>.services.ai.azure.com/api/projects/<project-name>");
            }

            // Validate that endpoint contains /api/projects/ path
            var path = endpointUri.AbsolutePath.TrimEnd('/');
            var projectsIndex = path.IndexOf("/api/projects/", StringComparison.OrdinalIgnoreCase);
            if (projectsIndex < 0)
            {
                throw new ValidationException(
                    $"The endpoint must be a Microsoft Foundry project endpoint containing '/api/projects/<project-name>'. " +
                    $"Current endpoint: {foundrySettings.Endpoint}. " +
                    $"Expected format: https://<resource>.services.ai.azure.com/api/projects/<project-name>");
            }

            // Ensure there's a project name after /api/projects/
            var afterProjects = path[(projectsIndex + "/api/projects/".Length)..];
            if (string.IsNullOrWhiteSpace(afterProjects))
            {
                throw new ValidationException(
                    $"The endpoint must include a project name after '/api/projects/'. " +
                    $"Current endpoint: {foundrySettings.Endpoint}. " +
                    $"Expected format: https://<resource>.services.ai.azure.com/api/projects/<project-name>");
            }
        }

        // Validate deployment names are provided
        if (string.IsNullOrWhiteSpace(Settings.MicrosoftFoundry?.ChatCompletion?.DeploymentName))
        {
            throw new ValidationException("ChatCompletion.DeploymentName is required.");
        }

        if (string.IsNullOrWhiteSpace(Settings.MicrosoftFoundry?.Embedding?.DeploymentName))
        {
            throw new ValidationException("Embedding.DeploymentName is required.");
        }

        // Validate authentication-specific requirements
        if (foundrySettings.AuthenticationType == AuthenticationType.ApiKey)
        {
            if (string.IsNullOrWhiteSpace(foundrySettings.ApiKey))
            {
                throw new ValidationException("ApiKey is required when using ApiKey authentication.");
            }
        }
    }
}