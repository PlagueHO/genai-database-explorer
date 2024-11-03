using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// This class implements the <see cref="ICommandHandler"/> interface and provides functionality to handle query commands.
/// </remarks>
public class QueryCommandHandler(ILogger<ICommandHandler> logger) : CommandHandler(logger)
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