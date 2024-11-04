using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Models.SemanticModel;

public sealed class SemanticModel : ISemanticModel
{
    public SemanticModel(
        string name,
        string source,
        IEnumerable<SemanticModelTable>? tables = null,
        IEnumerable<SemanticModelView>? views = null,
        IEnumerable<SemanticModelStoredProcedure>? storedProcedures = null,
        string? description = null)
    {
        Name = name;
        Source = source;
        Tables = tables ?? [];
        Views = views ?? [];
        StoredProcedures = storedProcedures ?? [];
        Description = description;
    }

    public string Name { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; }

    public string Source { get; }

    public IEnumerable<SemanticModelTable> Tables { get; }

    public IEnumerable<SemanticModelView> Views { get; }

    public IEnumerable<SemanticModelStoredProcedure> StoredProcedures { get; }
}
