using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
            SemanticModelRepository = new SemanticModelRepositorySettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration?.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration?.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration?.GetSection(DataDictionarySettings.PropertyName).Bind(Settings.DataDictionary);
        _configuration?.GetSection(SemanticModelSettings.PropertyName).Bind(Settings.SemanticModel);
        _configuration?.GetSection(OpenAIServiceSettings.PropertyName).Bind(Settings.OpenAIService);
        _configuration?.GetSection(SemanticModelRepositorySettings.PropertyName).Bind(Settings.SemanticModelRepository);

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

        validationContext = new ValidationContext(Settings.OpenAIService);
        Validator.ValidateObject(Settings.OpenAIService, validationContext, validateAllProperties: true);
        logger.LogInformation("{Message} '{Section}'", _resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "OpenAIService");

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

            case "cosmos":
                if (Settings.SemanticModelRepository.CosmosDb == null)
                {
                    throw new ValidationException("CosmosDb configuration is required when PersistenceStrategy is 'Cosmos'.");
                }
                var cosmosContext = new ValidationContext(Settings.SemanticModelRepository.CosmosDb);
                Validator.ValidateObject(Settings.SemanticModelRepository.CosmosDb, cosmosContext, validateAllProperties: true);
                break;

            default:
                throw new ValidationException($"Invalid PersistenceStrategy '{strategy}'. Valid values are: LocalDisk, AzureBlob, Cosmos.");
        }
    }
}