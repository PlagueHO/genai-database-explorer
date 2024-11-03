using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to build.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class BuildCommandHandler(IProject project, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(project, serviceProvider, logger)
{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        const string logMessageTemplate = "Building project at '{ProjectPath}'.";
        _logger.LogInformation(logMessageTemplate, projectPath.FullName);

        // Continue with the build process...
    }
}
