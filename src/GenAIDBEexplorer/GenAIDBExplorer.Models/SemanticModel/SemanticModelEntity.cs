using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a SQL entity in the semantic model.
/// </summary>
public abstract class SemanticModelEntity(
    string schema,
    string name,
    string? description = null
) : ISemanticModelEntity
{
    protected static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Gets or sets the schema of the entity.
    /// </summary>
    public string Schema { get; set; } = schema;

    /// <summary>
    /// Gets or sets the name of the entity.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the description of the entity.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets or sets a value indicating whether the entity should be ignored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsIgnored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating why the entity is ignored (if ignored).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? IgnoreReason { get; set; }

    /// <summary>
    /// Gets or sets the semantic description of the entity.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SemanticDescription { get; set; }

    /// <summary>
    /// Saves the semantic model entity to the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be saved.</param>
    public void SaveModel(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);

        File.WriteAllText(filePath, JsonSerializer.Serialize<object>(this, _jsonSerializerOptions));
    }
}
