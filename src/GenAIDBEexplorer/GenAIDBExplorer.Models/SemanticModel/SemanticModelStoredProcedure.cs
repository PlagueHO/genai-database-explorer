using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a stored procedure in the semantic model.
/// </summary>
public sealed class SemanticModelStoredProcedure(
    string schema,
    string name,
    string parameters,
    string definition,
    string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets or sets the parameters of the stored procedure.
    /// </summary>
    public string Parameters { get; set; } = parameters;

    /// <summary>
    /// Gets or sets the definition of the stored procedure.
    /// </summary>
    public string Definition { get; set; } = definition;
}