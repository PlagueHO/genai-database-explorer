using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Models.Project
{
    public class Project : IProject
    {
        private readonly ILogger<Project> _logger;

        private readonly IConfiguration _configuration;

        public ProjectSettings Settings { get; private set; }

        public Project(IConfiguration configuration, ILogger<Project> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Initialize ProjectSettings and bind configuration sections
            Settings = new ProjectSettings();

            // Read the SettingsVersion
            Settings.SettingsVersion = _configuration.GetValue<Version>(nameof(Settings.SettingsVersion));

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
}