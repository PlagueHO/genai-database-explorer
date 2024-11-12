namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Build command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
public class BuildCommandHandlerOptions(DirectoryInfo projectPath) : CommandHandlerOptions(projectPath)
{
    // Additional properties specific to BuildCommandHandler can be added here
}
