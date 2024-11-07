using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model
/// </summary>
internal interface ISemanticModel : ISemanticModelItem
{
    /// <summary>
    /// Gets the source of the semantic model
    /// </summary>
    public string Source { get; set; }
}
