using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents an index in the semantic model.
/// </summary>
public sealed class SemanticModelIndex(
        string schema,
        string name,
        string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets the schema of the index.
    /// </summary>
    public new string Schema { get; set; } = schema;

    /// <summary>
    /// Gets the name of the index.
    /// </summary>
    public new string Name { get; set; } = name;

    /// <summary>
    /// Gets the description of the index.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public new string? Description { get; set; } = description;

    /// <summary>
    /// Gets the type of the index (clustered, nonclustered, etc.).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the column 
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is a primary key.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is a unique constraint.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsUniqueConstraint { get; set; }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitIndex(this);
    }
}
