using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a view in the semantic model.
/// </summary>
public sealed class SemanticModelView(
    string name,
    string? description = null
    ) : ISemanticModelItem
{
    /// <summary>
    /// Gets or sets the name of the view.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the description of the view.
    /// </summary>
    public string? Description { get; set; } = description;
}