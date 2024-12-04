namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Extract Model command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExtractModelCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
/// <param name="skipTables">Flag to skip tables during the extract model process.</param>
/// <param name="skipViews">Flag to skip views during the extract model process.</param>
/// <param name="skipStoredProcedures">Flag to skip stored procedures during the extract model process.</param>
public class ExtractModelCommandHandlerOptions(
    DirectoryInfo projectPath,
    bool skipTables = false,
    bool skipViews = false,
    bool skipStoredProcedures = false
) : CommandHandlerOptions(projectPath)
{
    public bool SkipTables { get; } = skipTables;
    public bool SkipViews { get; } = skipViews;
    public bool SkipStoredProcedures { get; } = skipStoredProcedures;
}
