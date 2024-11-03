using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandler"/> class.
/// </remarks>
/// <param name="projectFactory">The project factory instance for creating project instances.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class BuildCommandHandler(IProjectFactory projectFactory, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(projectFactory, serviceProvider, logger)

{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        const string logMessageTemplate = "Building project at '{ProjectPath}'.";
        _logger.LogInformation(logMessageTemplate, projectPath.FullName);

        // Get the IProject service instance using the factory
        var project = _projectFactory.Create(projectPath);

        // Continue with the build process...
    }
}
