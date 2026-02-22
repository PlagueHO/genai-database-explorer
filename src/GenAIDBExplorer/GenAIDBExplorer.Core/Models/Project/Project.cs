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
            FoundryModels = new FoundryModelsSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration?.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration?.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration?.GetSection(DataDictionarySettings.PropertyName).Bind(Settings.DataDictionary);
        _configuration?.GetSection(SemanticModelSettings.PropertyName).Bind(Settings.SemanticModel);
        _configuration?.GetSection(FoundryModelsSettings.PropertyName).Bind(Settings.FoundryModels);
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

        validationContext = new ValidationContext(Settings.FoundryModels);
        Validator.ValidateObject(Settings.FoundryModels, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "FoundryModels");

        // Validate Foundry Models-specific configuration
        ValidateFoundryModelsConfiguration();

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
    /// Validates Foundry Models configuration to ensure endpoints are valid URIs and required settings are present.
    /// </summary>
    private void ValidateFoundryModelsConfiguration()
    {
        // Detect old OpenAIService section and provide migration guidance
        if (_configuration?.GetSection("OpenAIService").Exists() == true)
        {
            throw new ValidationException(
                "The 'OpenAIService' configuration section has been replaced by 'FoundryModels'. " +
                "Please update your settings.json file. See documentation for the new configuration format.");
        }

        var foundrySettings = Settings.FoundryModels?.Default;
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
                    "Expected format: https://your-resource.services.ai.azure.com/");
            }

            // Validate that it's an HTTPS URL
            if (endpointUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ValidationException(
                    $"Endpoint must use HTTPS. Current URL: {foundrySettings.Endpoint}");
            }

            // Validate that it looks like a valid Foundry Models endpoint
            var host = endpointUri.Host.ToLowerInvariant();
            if (!host.EndsWith(".services.ai.azure.com") &&
                !host.EndsWith(".openai.azure.com") &&
                !host.EndsWith(".cognitiveservices.azure.com"))
            {
                throw new ValidationException(
                    $"Endpoint '{foundrySettings.Endpoint}' does not appear to be a valid Foundry Models endpoint. " +
                    "Expected hostname ending with .services.ai.azure.com, .openai.azure.com, or .cognitiveservices.azure.com");
            }
        }

        // Validate deployment names are provided
        if (string.IsNullOrWhiteSpace(Settings.FoundryModels?.ChatCompletion?.DeploymentName))
        {
            throw new ValidationException("ChatCompletion.DeploymentName is required.");
        }

        if (string.IsNullOrWhiteSpace(Settings.FoundryModels?.ChatCompletionStructured?.DeploymentName))
        {
            throw new ValidationException("ChatCompletionStructured.DeploymentName is required.");
        }

        if (string.IsNullOrWhiteSpace(Settings.FoundryModels?.Embedding?.DeploymentName))
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