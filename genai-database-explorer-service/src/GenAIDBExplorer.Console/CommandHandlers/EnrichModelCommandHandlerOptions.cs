using GenAIDBExplorer.Console.CommandHandlers;

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
/// <param name="objectType">The type of the object to enrich (table, view, or storedprocedure).</param>
/// <param name="schemaName">The schema name of the object to enrich.</param>
/// <param name="objectName">The name of the object to enrich.</param>
public class EnrichModelCommandHandlerOptions(
    DirectoryInfo projectPath,
    bool skipTables = false,
    bool skipViews = false,
    bool skipStoredProcedures = false,
    string? objectType = null,
    string? schemaName = null,
    string? objectName = null,
    bool show = false
) : CommandHandlerOptions(projectPath)
{
    public bool SkipTables { get; } = skipTables;
    public bool SkipViews { get; } = skipViews;
    public bool SkipStoredProcedures { get; } = skipStoredProcedures;
    public string? ObjectType { get; } = objectType;
    public string? SchemaName { get; } = schemaName;
    public string? ObjectName { get; } = objectName;
    public bool Show { get; } = show;
}
