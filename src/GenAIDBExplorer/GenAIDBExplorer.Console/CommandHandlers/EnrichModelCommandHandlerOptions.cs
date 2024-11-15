namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Enrich Model command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EnrichModelCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
/// <param name="skipTables">Flag to skip tables during the description generation process.</param>
/// <param name="skipViews">Flag to skip views during the description generation process.</param>
/// <param name="skipStoredProcedures">Flag to skip stored procedures during the description generation process.</param>
/// <param name="singleModelFile">Flag to save the semantic model as a single file.</param>
public class EnrichModelCommandHandlerOptions(
    DirectoryInfo projectPath,
    bool skipTables = false,
    bool skipViews = false,
    bool skipStoredProcedures = false,
    bool singleModelFile = false
) : CommandHandlerOptions(projectPath)
{
    public bool SkipTables { get; } = skipTables;
    public bool SkipViews { get; } = skipViews;
    public bool SkipStoredProcedures { get; } = skipStoredProcedures;
    public bool SingleModelFile { get; } = singleModelFile;
}
