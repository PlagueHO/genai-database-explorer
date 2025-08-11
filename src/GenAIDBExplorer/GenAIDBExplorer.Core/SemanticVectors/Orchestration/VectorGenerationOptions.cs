using System;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Options controlling vector generation and reconciliation.
/// </summary>
public sealed class VectorGenerationOptions
{
    /// <summary>
    /// If true, existing embeddings will be overwritten.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// If true, performs a dry run without writing changes.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// If true, skips table entities.
    /// </summary>
    public bool SkipTables { get; init; }

    /// <summary>
    /// If true, skips view entities.
    /// </summary>
    public bool SkipViews { get; init; }

    /// <summary>
    /// If true, skips stored procedure entities.
    /// </summary>
    public bool SkipStoredProcedures { get; init; }

    /// <summary>
    /// Restricts processing to a specific object type (table, view, or storedprocedure).
    /// </summary>
    public string? ObjectType { get; init; } // table|view|storedprocedure

    /// <summary>
    /// Restricts processing to a specific schema name.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <summary>
    /// Restricts processing to a specific object name.
    /// </summary>
    public string? ObjectName { get; init; }
}
