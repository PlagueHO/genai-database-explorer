namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Init command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
public class InitCommandHandlerOptions(DirectoryInfo projectPath) : CommandHandlerOptions(projectPath)
{
    // Additional properties specific to InitCommandHandler can be added here
}
