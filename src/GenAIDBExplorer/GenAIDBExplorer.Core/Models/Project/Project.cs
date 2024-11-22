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
    /// Logger instance for logging information, warnings, and errors.
    /// </summary>
    private readonly ILogger<Project> _logger = logger;

    /// <summary>
    /// Configuration instance for accessing project settings.
    /// </summary>
    private IConfiguration _configuration;

    /// <summary>
    /// Gets the project settings.
    /// </summary>
    public ProjectSettings Settings { get; private set; }

    /// <summary>
    /// Gets the project directory.
    /// </summary>
    public DirectoryInfo ProjectDirectory { get; private set; }

    /// <summary>
    /// Initializes the project directory by copying the default project structure.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    public void InitializeProjectDirectory(DirectoryInfo projectDirectory)
    {
        ProjectDirectory = projectDirectory;

        if (ProjectUtils.IsDirectoryNotEmpty(projectDirectory))
        {
            _logger.LogError(_resourceManagerErrorMessages.GetString("ErrorProjectFolderNotEmpty"));

            // Throw exception directory is not empty
            throw new InvalidOperationException(_resourceManagerErrorMessages.GetString("ErrorProjectFolderNotEmpty"));
        }

        var defaultProjectDirectory = new DirectoryInfo("DefaultProject");
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
            ChatCompletion = new ChatCompletionSettings(),
            ChatCompletionStructured = new ChatCompletionStructuredSettings(),
            Embedding = new EmbeddingSettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration.GetSection(ChatCompletionSettings.PropertyName).Bind(Settings.ChatCompletion);
        _configuration.GetSection(ChatCompletionStructuredSettings.PropertyName).Bind(Settings.ChatCompletionStructured);
        _configuration.GetSection(EmbeddingSettings.PropertyName).Bind(Settings.Embedding);

        ValidateSettings();
    }

    /// <summary>
    /// Validates the project settings.
    /// </summary>
    private void ValidateSettings()
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationStarted"));

        var validationContext = new ValidationContext(Settings.Database);
        Validator.ValidateObject(Settings.Database, validationContext, validateAllProperties: true);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "Database");

        validationContext = new ValidationContext(Settings.ChatCompletion);
        Validator.ValidateObject(Settings.ChatCompletion, validationContext, validateAllProperties: true);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "ChatCompletion");

        validationContext = new ValidationContext(Settings.ChatCompletionStructured);
        Validator.ValidateObject(Settings.ChatCompletionStructured, validationContext, validateAllProperties: true);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "ChatCompletionStructured");

        validationContext = new ValidationContext(Settings.Embedding);
        Validator.ValidateObject(Settings.Embedding, validationContext, validateAllProperties: true);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationSuccessful"), "Embedding");

        _logger.LogInformation(_resourceManagerLogMessages.GetString("ProjectSettingsValidationCompleted"));
    }
}