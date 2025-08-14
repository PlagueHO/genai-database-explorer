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
            OpenAIService = new OpenAIServiceSettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration?.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration?.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration?.GetSection(DataDictionarySettings.PropertyName).Bind(Settings.DataDictionary);
        _configuration?.GetSection(SemanticModelSettings.PropertyName).Bind(Settings.SemanticModel);
        _configuration?.GetSection(OpenAIServiceSettings.PropertyName).Bind(Settings.OpenAIService);
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

        validationContext = new ValidationContext(Settings.OpenAIService);
        Validator.ValidateObject(Settings.OpenAIService, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "OpenAIService");

        // Validate OpenAI service-specific configuration
        ValidateOpenAIConfiguration();

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
                if (Settings.SemanticModelRepository.AzureBlobStorage == null)
                {
                    throw new ValidationException("AzureBlobStorage configuration is required when PersistenceStrategy is 'AzureBlob'.");
                }
                var azureBlobContext = new ValidationContext(Settings.SemanticModelRepository.AzureBlobStorage);
                Validator.ValidateObject(Settings.SemanticModelRepository.AzureBlobStorage, azureBlobContext, validateAllProperties: true);
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
    /// Validates OpenAI service configuration to ensure endpoints are valid URIs and required settings are present.
    /// </summary>
    private void ValidateOpenAIConfiguration()
    {
        var openAISettings = Settings.OpenAIService?.Default;
        if (openAISettings == null)
        {
            return; // Basic validation will catch this
        }

        // Validate Azure OpenAI endpoint URL format
        if (openAISettings.ServiceType == "AzureOpenAI" && !string.IsNullOrEmpty(openAISettings.AzureOpenAIEndpoint))
        {
            if (!Uri.TryCreate(openAISettings.AzureOpenAIEndpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new ValidationException(
                    $"AzureOpenAIEndpoint '{openAISettings.AzureOpenAIEndpoint}' is not a valid URL. " +
                    "Expected format: https://your-resource.cognitiveservices.azure.com/");
            }

            // Validate that it's an HTTPS URL
            if (endpointUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ValidationException(
                    $"AzureOpenAIEndpoint must use HTTPS. Current URL: {openAISettings.AzureOpenAIEndpoint}");
            }

            // Validate that it looks like an Azure endpoint
            var host = endpointUri.Host.ToLowerInvariant();
            if (!host.EndsWith(".cognitiveservices.azure.com") && !host.EndsWith(".openai.azure.com"))
            {
                throw new ValidationException(
                    $"AzureOpenAIEndpoint '{openAISettings.AzureOpenAIEndpoint}' does not appear to be a valid Azure OpenAI or Azure Cognitive Services endpoint. " +
                    "Expected format: https://your-resource.cognitiveservices.azure.com/ or https://your-resource.openai.azure.com/");
            }
        }

        // Validate deployment IDs are provided when using Azure OpenAI
        if (openAISettings.ServiceType == "AzureOpenAI")
        {
            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.ChatCompletion?.AzureOpenAIDeploymentId))
            {
                throw new ValidationException("ChatCompletion.AzureOpenAIDeploymentId is required when using Azure OpenAI service.");
            }

            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.ChatCompletionStructured?.AzureOpenAIDeploymentId))
            {
                throw new ValidationException("ChatCompletionStructured.AzureOpenAIDeploymentId is required when using Azure OpenAI service.");
            }

            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.Embedding?.AzureOpenAIDeploymentId))
            {
                throw new ValidationException("Embedding.AzureOpenAIDeploymentId is required when using Azure OpenAI service.");
            }
        }

        // Validate model IDs are provided when using OpenAI
        if (openAISettings.ServiceType == "OpenAI")
        {
            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.ChatCompletion?.ModelId))
            {
                throw new ValidationException("ChatCompletion.ModelId is required when using OpenAI service.");
            }

            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.ChatCompletionStructured?.ModelId))
            {
                throw new ValidationException("ChatCompletionStructured.ModelId is required when using OpenAI service.");
            }

            if (string.IsNullOrWhiteSpace(Settings.OpenAIService?.Embedding?.ModelId))
            {
                throw new ValidationException("Embedding.ModelId is required when using OpenAI service.");
            }
        }
    }
}