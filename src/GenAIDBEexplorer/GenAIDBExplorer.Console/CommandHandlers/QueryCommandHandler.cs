using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryCommandHandler"/> class.
/// </remarks>
/// <param name="projectFactory">The project factory instance for creating project instances.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class QueryCommandHandler(IProjectFactory projectFactory, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger )
    : CommandHandler(projectFactory, serviceProvider, logger)
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