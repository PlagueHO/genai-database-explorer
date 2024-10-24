using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

public sealed class SemanticModelStoredProcedure : ISemanticModelItem
{
    SemanticModelItemType ISemanticModelItem.ItemType => SemanticModelItemType.StoredProcedure;

    public SemanticModelStoredProcedure(
        string name,
        string source,
        string? description = null)
    {
        Name = name;
        Description = description;
        Source = source;
    }

    public string Name { get; }

    public string? Description { get; }

    public string Source { get; }
}
