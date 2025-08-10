using System;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Options controlling vector generation/reconciliation.
/// </summary>
public sealed class VectorGenerationOptions
{
    public bool Overwrite { get; init; }
    public bool DryRun { get; init; }

    // Selection
    public bool SkipTables { get; init; }
    public bool SkipViews { get; init; }
    public bool SkipStoredProcedures { get; init; }

    public string? ObjectType { get; init; } // table|view|storedprocedure
    public string? SchemaName { get; init; }
    public string? ObjectName { get; init; }
}
