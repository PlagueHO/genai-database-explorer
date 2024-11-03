using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryCommandHandler"/> class.
/// </remarks>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
public class QueryCommandHandler(ILogger<ICommandHandler> logger, IServiceProvider serviceProvider) : CommandHandler(logger, serviceProvider)
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