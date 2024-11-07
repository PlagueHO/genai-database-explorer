using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model item
/// </summary>
internal interface ISemanticModelItem
{
    /// <summary>
    /// Gets the name of the semantic model item.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the description of the semantic model item.
    /// </summary>
    public string? Description { get; set; }
}