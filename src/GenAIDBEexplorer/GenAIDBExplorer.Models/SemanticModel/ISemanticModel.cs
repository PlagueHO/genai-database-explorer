using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model
/// </summary>
internal interface ISemanticModel
{
    /// <summary>
    /// The name of the semantic model
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of the semantic model
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// The source of the semantic model
    /// This is the database connection string
    /// </summary>
    public string Source { get; }

}
