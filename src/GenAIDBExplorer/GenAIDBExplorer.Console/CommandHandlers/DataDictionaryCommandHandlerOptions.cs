using GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Data Dictionary command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DataDictionaryCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the GenAI Database Explorer project.</param>
/// <param name="sourcePath">The path to the source directory containing data dictionary files.</param>
/// <param name="objectType">The type of the object to process (e.g., table).</param>
/// <param name="schemaName">The schema name of the object to process.</param>
/// <param name="objectName">The name of the object to process.</param>
/// <param name="show">Flag to display the entity after processing.</param>
public class DataDictionaryCommandHandlerOptions(
    DirectoryInfo projectPath,
    string sourcePathPattern,
    string? objectType = null,
    string? schemaName = null,
    string? objectName = null,
    bool show = false
) : CommandHandlerOptions(projectPath)
{
    public string SourcePathPattern { get; } = sourcePathPattern;
    public string? ObjectType { get; } = objectType;
    public string? SchemaName { get; } = schemaName;
    public string? ObjectName { get; } = objectName;
    public bool Show { get; } = show;
}
