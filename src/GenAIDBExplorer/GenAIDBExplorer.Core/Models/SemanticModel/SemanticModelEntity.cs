using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

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
    /// Gets or sets the semantic description of the entity.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [YamlDotNet.Serialization.YamlIgnore]
    public string? SemanticDescription { get; set; }

    /// <summary>
    /// Gets or sets the last update date of the semantic description.
    /// </summary>    
    [YamlDotNet.Serialization.YamlIgnore]
    public DateTime? SemanticDescriptionLastUpdate { get; set; }

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
    /// Saves the semantic model entity to the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be saved.</param>
    public async Task SaveModelAsync(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize<object>(this, _jsonSerializerOptions));
    }

    /// <summary>
    /// Loads the semantic model entity from the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be loaded from.</param>
    public async Task LoadModelAsync(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);
        if (File.Exists(filePath))
        {
            await using var stream = File.OpenRead(filePath);
            var entity = await JsonSerializer.DeserializeAsync<SemanticModelEntity>(stream, _jsonSerializerOptions);
            Schema = entity.Schema;
            Name = entity.Name;
            Description = entity.Description;
            SemanticDescription = entity.SemanticDescription;
            IsIgnored = entity.IsIgnored;
            IgnoreReason = entity.IgnoreReason;
        }
    }

    /// <summary>
    /// Gets the filename of the model entity if the model is split.
    /// </summary>
    /// <returns>The filename of the model entity.</returns>
    public FileInfo GetModelEntityFilename()
    {
        return new FileInfo($"{Schema}.{Name}.json");
    }

    /// <summary>
    /// Gets the path to the model entity if the model is split.
    /// </summary>
    /// <returns>The relative path to the model entity.</returns>
    public abstract DirectoryInfo GetModelPath();

    /// <summary>
    /// Sets the semantic description of the entity.
    /// </summary>
    /// <param name="semanticDescription"></param>
    public void SetSemanticDescription(string semanticDescription)
    {
        SemanticDescription = semanticDescription;
        SemanticDescriptionLastUpdate = DateTime.Now;
    }
}
