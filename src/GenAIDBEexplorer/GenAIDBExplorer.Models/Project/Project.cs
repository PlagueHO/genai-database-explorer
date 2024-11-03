using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

public class Project(ILogger<Project> logger) : IProject
{
    private readonly ILogger<Project> _logger = logger;
    private IConfiguration _configuration;

    public ProjectSettings Settings { get; private set; }

    public void LoadConfiguration(string projectPath)
    {
        // Create IConfiguration from the projectPath
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectPath)
            .AddJsonFile("settings.json", optional: false, reloadOnChange: false);

        configurationBuilder.Build();

        InitializeSettings();
    }

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

    private void ValidateSettings()
    {
        _logger.LogInformation("Starting project settings validation.");

        var validationContext = new ValidationContext(Settings.Database);
        Validator.ValidateObject(Settings.Database, validationContext, validateAllProperties: true);

        _logger.LogInformation("Database project settings validated successfully.");

        validationContext = new ValidationContext(Settings.ChatCompletion);
        Validator.ValidateObject(Settings.ChatCompletion, validationContext, validateAllProperties: true);

        _logger.LogInformation("ChatCompletion project settings validated successfully.");

        validationContext = new ValidationContext(Settings.Embedding);
        Validator.ValidateObject(Settings.Embedding, validationContext, validateAllProperties: true);

        _logger.LogInformation("Embedding project settings validated successfully.");

        _logger.LogInformation("Project settings validation completed.");
    }
}