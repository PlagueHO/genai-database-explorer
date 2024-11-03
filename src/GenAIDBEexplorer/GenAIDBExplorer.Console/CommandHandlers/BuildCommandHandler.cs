using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// This class implements the <see cref="ICommandHandler"/> interface and provides functionality to handle build commands.
/// </remarks>
public class BuildCommandHandler(ILogger<ICommandHandler> logger) : CommandHandler(logger)
{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation($"Building project at '{projectPath.FullName}'.");

        // Your build logic here
    }
}