using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

public sealed class SemanticModelTable : ISemanticModelItem
{
    SemanticModelItemType ISemanticModelItem.ItemType => SemanticModelItemType.Table;

    public SemanticModelTable(
        string name,
        string? description = null,
        IEnumerable<SemanticModelColumn>? columns = null)
    {
        Name = name;
        Description = description;
        Columns = columns ?? Array.Empty<SemanticModelColumn>();
    }

    public string Name { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; }

    public IEnumerable<SemanticModelColumn> Columns { get; }
}
