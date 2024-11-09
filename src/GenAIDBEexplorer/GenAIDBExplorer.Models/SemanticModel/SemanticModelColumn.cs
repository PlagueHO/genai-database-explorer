﻿using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a column in the semantic model.
/// </summary>
public sealed class SemanticModelColumn(
    string name,
    string type
    ) : ISemanticModelItem
{
    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the type of the column.
    /// </summary>
    public string Type { get; set; } = type;

    /// <summary>
    /// Gets the description of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

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
    public bool? IsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is an identity column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsIdentity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is computed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsComputed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is an XML document.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsXmlDocument { get; set; }

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
}