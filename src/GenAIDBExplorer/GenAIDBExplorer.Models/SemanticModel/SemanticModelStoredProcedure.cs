using System.Text.Json;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a stored procedure in the semantic model.
/// </summary>
public sealed class SemanticModelStoredProcedure(
    string schema,
    string name,
    string definition,
    string? parameters = null,
    string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets or sets the parameters of the stored procedure.
    /// </summary>
    public string? Parameters { get; set; } = parameters;

    /// <summary>
    /// Gets or sets the definition of the stored procedure.
    /// </summary>
    public string Definition { get; set; } = definition;
}