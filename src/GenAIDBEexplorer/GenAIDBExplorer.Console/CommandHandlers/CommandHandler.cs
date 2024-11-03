using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Abstract base class for command handlers.
/// </summary>
/// <remarks>
/// This class provides common utility functionality for handling console commands.
/// </remarks>
public abstract class CommandHandler(ILogger<ICommandHandler> logger) : ICommandHandler
{
    /// <summary>
    /// Logger instance for logging information, warnings, and errors.
    /// </summary>
    protected readonly ILogger<ICommandHandler> _logger = logger;

    /// <summary>
    /// Handles the command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to handle.</param>
    public abstract void Handle(DirectoryInfo projectPath);

    /// <summary>
    /// Validates the specified project path.
    /// </summary>
    /// <param name="projectPath">The project path to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the project path is null.</exception>
    protected static void ValidateProjectPath(DirectoryInfo projectPath)
    {
        if (projectPath == null)
        {
            throw new ArgumentNullException(nameof(projectPath), "Project path cannot be null.");
        }
    }
}
