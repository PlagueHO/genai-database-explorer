using GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Build command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
/// <param name="ignoreTables">Flag to ignore tables during the build process.</param>
/// <param name="ignoreViews">Flag to ignore views during the build process.</param>
/// <param name="ignoreStoredProcedures">Flag to ignore stored procedures during the build process.</param>
public class BuildCommandHandlerOptions(
    DirectoryInfo projectPath,
    bool ignoreTables = false,
    bool ignoreViews = false,
    bool ignoreStoredProcedures = false
) : CommandHandlerOptions(projectPath)
{
    public bool IgnoreTables { get; } = ignoreTables;
    public bool IgnoreViews { get; } = ignoreViews;
    public bool IgnoreStoredProcedures { get; } = ignoreStoredProcedures;
}
