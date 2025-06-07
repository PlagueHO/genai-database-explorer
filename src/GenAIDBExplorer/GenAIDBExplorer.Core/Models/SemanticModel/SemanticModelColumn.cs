using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a column in the semantic model.
/// </summary>
public sealed class SemanticModelColumn(string schema, string name, string? description = null)
    : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public new string Schema { get; set; } = schema;

    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public new string Name { get; set; } = name;

    /// <summary>
    /// Gets the description of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public new string? Description { get; set; } = description;

    /// <summary>
    /// Gets the type of the column.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets a value indicating whether the column is a primary key.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the precision of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is nullable.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is an identity column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is computed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsComputed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is an XML document.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsXmlDocument { get; set; }

    /// <summary>
    /// Gets the name of the referenced table, if any.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedTable { get; set; }

    /// <summary>
    /// Gets the name of the referenced column, if any.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedColumn { get; set; }

    /// <summary>
    /// Saving a column to a folder is not implemented.
    /// </summary>
    /// <param name="folderPath"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SaveModel(DirectoryInfo folderPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitColumn(this);
    }
}