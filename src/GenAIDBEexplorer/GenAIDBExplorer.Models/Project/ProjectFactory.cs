using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Models.Project
{
    public class ProjectFactory(IServiceProvider serviceProvider, ILogger<ProjectFactory> logger) : IProjectFactory
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<ProjectFactory> _logger = logger;

        public IProject Create(DirectoryInfo projectPath)
        {
            // Log the project creation
            const string logMessageTemplate = "Creating project at '{ProjectPath}'.";
            _logger.LogInformation(logMessageTemplate, projectPath.FullName);

            // Create the project instance
            var project = _serviceProvider.GetRequiredService<IProject>();
            return project;
        }
    }
}
