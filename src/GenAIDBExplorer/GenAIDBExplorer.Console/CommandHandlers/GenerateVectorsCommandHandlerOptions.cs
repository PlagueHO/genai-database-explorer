using System.IO;

namespace GenAIDBExplorer.Console.CommandHandlers;

public sealed class GenerateVectorsCommandHandlerOptions(DirectoryInfo projectPath,
    bool overwrite = false,
    bool dryRun = false,
    bool skipTables = false,
    bool skipViews = false,
    bool skipStoredProcedures = false,
    string? objectType = null,
    string? schemaName = null,
    string? objectName = null) : ICommandHandlerOptions
{
    public DirectoryInfo ProjectPath { get; } = projectPath;
    public bool Overwrite { get; } = overwrite;
    public bool DryRun { get; } = dryRun;
    public bool SkipTables { get; } = skipTables;
    public bool SkipViews { get; } = skipViews;
    public bool SkipStoredProcedures { get; } = skipStoredProcedures;
    public string? ObjectType { get; } = objectType;
    public string? SchemaName { get; } = schemaName;
    public string? ObjectName { get; } = objectName;
}
