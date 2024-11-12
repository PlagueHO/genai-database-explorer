namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the base class for command handler options.
/// </summary>
public abstract class CommandHandlerOptions(DirectoryInfo projectPath) : ICommandHandlerOptions
{
    /// <summary>
    /// Gets the path to the project directory.
    /// </summary>
    public DirectoryInfo ProjectPath { get; } = projectPath;
}
