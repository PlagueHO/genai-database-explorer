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
    /// Name of the semantic model item
    /// </summary>
    public string Name { get; }

    public string? Description { get; }

    public SemanticModelItemType ItemType { get; }
}
