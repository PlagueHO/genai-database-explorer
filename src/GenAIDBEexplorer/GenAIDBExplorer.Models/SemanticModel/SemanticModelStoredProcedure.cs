using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a stored procedure in the semantic model.
/// </summary>
public sealed class SemanticModelStoredProcedure(
    string name,
    string source,
    string? description = null
    ) : ISemanticModelItem
{
    /// <summary>
    /// Gets or sets the name of the stored procedure.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the description of the stored procedure.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets or sets the source of the stored procedure.
    /// </summary>
    public string Source { get; set; } = source;
}