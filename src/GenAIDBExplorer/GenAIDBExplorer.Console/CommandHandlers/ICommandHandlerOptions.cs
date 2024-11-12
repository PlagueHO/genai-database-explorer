namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options required for handling commands.
/// </summary>
public interface ICommandHandlerOptions
{
    /// <summary>
    /// Gets the path to the project directory.
    /// </summary>
    DirectoryInfo ProjectPath { get; }
}
