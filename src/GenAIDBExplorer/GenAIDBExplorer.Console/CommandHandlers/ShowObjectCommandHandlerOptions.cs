namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Show command handler.
/// </summary>
/// <param name="ProjectPath">The path to the project directory.</param>
/// <param name="SchemaName">The schema name of the object to show.</param>
/// <param name="ObjectName">The name of the object to show.</param>
/// <param name="ObjectType">The type of the object to show (table, view, storedprocedure).</param>
public class ShowObjectCommandHandlerOptions(
    DirectoryInfo ProjectPath,
    string SchemaName,
    string ObjectName,
    string ObjectType
) : CommandHandlerOptions(ProjectPath)
{
    public string SchemaName { get; } = SchemaName;
    public string ObjectName { get; } = ObjectName;
    public string ObjectType { get; } = ObjectType;
}