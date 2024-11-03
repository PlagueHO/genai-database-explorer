namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Defines the contract for a command handler.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for handling commands with a specified project path.
/// </remarks>
public interface ICommandHandler
{
    /// <summary>
    /// Handles the command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to handle.</param>
    void Handle(DirectoryInfo projectPath);
}
