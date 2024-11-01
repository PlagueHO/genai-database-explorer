using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace GenAIDBExplorer.Models.Project
{
    public class Project : IProject
    {
        public IConfiguration Configuration { get; private set; }
        public ChatCompletionSettings ChatCompletionSettings { get; private set; }
        public DatabaseSettings DatabaseSettings { get; private set; }
        public EmbeddingSettings EmbeddingSettings { get; private set; }

        public Project(IConfiguration configuration)
        {
            Configuration = configuration;

            ChatCompletionSettings = Configuration.GetSection(ChatCompletionSettings.PropertyName).Get<ChatCompletionSettings>();
            DatabaseSettings = Configuration.GetSection(DatabaseSettings.PropertyName).Get<DatabaseSettings>();
            EmbeddingSettings = Configuration.GetSection(EmbeddingSettings.PropertyName).Get<EmbeddingSettings>();

            ValidateSettings();
        }

        private void ValidateSettings()
        {
            var validationContext = new ValidationContext(DatabaseSettings);
            Validator.ValidateObject(DatabaseSettings, validationContext, validateAllProperties: true);
        }
    }
}