using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

public sealed class SemanticModelView : ISemanticModelItem
{
    SemanticModelItemType ISemanticModelItem.ItemType => SemanticModelItemType.View;

    public SemanticModelView(
        string name,
        string? description = null)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string? Description { get; }
}
