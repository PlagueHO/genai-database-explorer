using System.IO;

namespace GenAIDBExplorer.Console.CommandHandlers;

public sealed class ReconcileIndexCommandHandlerOptions(DirectoryInfo projectPath, bool dryRun = true) : ICommandHandlerOptions
{
    public DirectoryInfo ProjectPath { get; } = projectPath;
    public bool DryRun { get; } = dryRun;
}
