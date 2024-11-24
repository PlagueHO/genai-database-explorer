namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Query command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryModelCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
public class QueryModelCommandHandlerOptions(DirectoryInfo projectPath) : CommandHandlerOptions(projectPath)
{
    // Additional properties specific to QueryCommandHandler can be added here
}
