using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Models.Project;

public class Project(ILogger<Project> logger) : IProject
{
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
    /// Initializes the project directory by copying the default project structure.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    public void InitializeProjectDirectory(DirectoryInfo projectDirectory)
    {
        if (ProjectUtils.IsDirectoryNotEmpty(projectDirectory))
        {
            _logger.LogError(LogMessages.ProjectFolderNotEmpty);

            // Throw exception directory is not empty
            throw new InvalidOperationException(LogMessages.ProjectFolderNotEmpty);
        }

        var defaultProjectDirectory = new DirectoryInfo("DefaultProject");
        ProjectUtils.CopyDirectory(defaultProjectDirectory, projectDirectory);
    }

    /// <summary>
    /// Loads the configuration from the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to load the configuration from.</param>
    public void LoadConfiguration(string projectPath)
    {
        // Create IConfiguration from the projectPath
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectPath)
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
            Embedding = new EmbeddingSettings()
        };

        // Read the SettingsVersion
        Settings.SettingsVersion = _configuration.GetValue<Version>(nameof(Settings.SettingsVersion)) ?? new Version();

        _configuration.GetSection(DatabaseSettings.PropertyName).Bind(Settings.Database);
        _configuration.GetSection(ChatCompletionSettings.PropertyName).Bind(Settings.ChatCompletion);
        _configuration.GetSection(EmbeddingSettings.PropertyName).Bind(Settings.Embedding);

        ValidateSettings();
    }

    /// <summary>
    /// Validates the project settings.
    /// </summary>
    private void ValidateSettings()
    {
        _logger.LogInformation(LogMessages.StartingProjectSettingsValidation);

        var validationContext = new ValidationContext(Settings.Database);
        Validator.ValidateObject(Settings.Database, validationContext, validateAllProperties: true);

        _logger.LogInformation(LogMessages.DatabaseProjectSettingsValidated);

        validationContext = new ValidationContext(Settings.ChatCompletion);
        Validator.ValidateObject(Settings.ChatCompletion, validationContext, validateAllProperties: true);

        _logger.LogInformation(LogMessages.ChatCompletionProjectSettingsValidated);

        validationContext = new ValidationContext(Settings.Embedding);
        Validator.ValidateObject(Settings.Embedding, validationContext, validateAllProperties: true);

        _logger.LogInformation(LogMessages.EmbeddingProjectSettingsValidated);

        _logger.LogInformation(LogMessages.ProjectSettingsValidationCompleted);
    }

    /// <summary>
    /// Contains log messages used in the <see cref="Project"/> class.
    /// </summary>
    internal static class LogMessages
    {
        internal const string ProjectFolderNotEmpty = "The project folder is not empty. Please specify an empty folder.";
        internal const string StartingProjectSettingsValidation = "Starting project settings validation.";
        internal const string DatabaseProjectSettingsValidated = "Database project settings validated successfully.";
        internal const string ChatCompletionProjectSettingsValidated = "ChatCompletion project settings validated successfully.";
        internal const string EmbeddingProjectSettingsValidated = "Embedding project settings validated successfully.";
        internal const string ProjectSettingsValidationCompleted = "Project settings validation completed.";
    }
}