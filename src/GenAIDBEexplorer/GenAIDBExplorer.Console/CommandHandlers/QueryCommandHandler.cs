using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to query.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class QueryCommandHandler(IProject project, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(project, serviceProvider, logger)
{
    /// <summary>
    /// Handles the query command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to query.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation($"Querying project at '{projectPath.FullName}'.");

        // Your query logic here
    }
}