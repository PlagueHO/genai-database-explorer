using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

public sealed class SemanticModelColumn : ISemanticModelItem
{
    public SemanticModelItemType ItemType = SemanticModelItemType.Column;

    public SemanticModelColumn(
        string name,
        string type,
        bool isPrimary,
        string? description = null,
        string? referencedTable = null,
        string? referencedColumn = null)
    {
        Name = name;
        Type = type;
        IsPrimary = isPrimary;
        Description = description;
        ReferencedTable = referencedTable;
        ReferencedColumn = referencedColumn;
    }
    public string Name { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; }

    public string Type { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPrimary { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedTable { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedColumn { get; }
}
