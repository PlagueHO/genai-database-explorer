using System.Text.Json;
using System.Text.Json.Serialization;
using GenAIDBExplorer.Models.SemanticModel.JsonConverters;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model for a database.
/// </summary>
public sealed class SemanticModel(
    string name,
    string source,
    string? description = null
    ) : ISemanticModel
{   
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    public string Source { get; set; } = source;

    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets the tables in the semantic model.
    /// </summary>
    public List<SemanticModelTable> Tables { get; set; } = [];

    /// <summary>
    /// Gets the views in the semantic model.
    /// </summary>
    public List<SemanticModelView> Views { get; set; } = [];

    /// <summary>
    /// Gets the stored procedures in the semantic model.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures { get; set; } = [];

    /// <summary>
    /// Adds a table to the semantic model.
    /// </summary>
    /// <param name="table">The table to add.</param>
    public void AddTable(SemanticModelTable table)
    {
        Tables.Add(table);
    }

    /// <summary>
    /// Removes a table from the semantic model.
    /// </summary>
    /// <param name="table">The table to remove.</param>
    /// <returns>True if the table was removed; otherwise, false.</returns>
    public bool RemoveTable(SemanticModelTable table)
    {
        return Tables.Remove(table);
    }

    /// <summary>
    /// Adds a view to the semantic model.
    /// </summary>
    /// <param name="view">The view to add.</param>
    public void AddView(SemanticModelView view)
    {
        Views.Add(view);
    }

    /// <summary>
    /// Removes a view from the semantic model.
    /// </summary>
    /// <param name="view">The view to remove.</param>
    /// <returns>True if the view was removed; otherwise, false.</returns>
    public bool RemoveView(SemanticModelView view)
    {
        return Views.Remove(view);
    }

    /// <summary>
    /// Adds a stored procedure to the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to add.</param>
    public void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        StoredProcedures.Add(storedProcedure);
    }

    /// <summary>
    /// Removes a stored procedure from the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to remove.</param>
    /// <returns>True if the stored procedure was removed; otherwise, false.</returns>
    public bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        return StoredProcedures.Remove(storedProcedure);
    }

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model will be saved.</param>
    /// <param name="splitModel">Flag to split the model into separate files.</param>
    public async Task SaveModelAsync(DirectoryInfo modelPath, bool splitModel = false)
    {
        // Save the semantic model to a JSON file.
        Directory.CreateDirectory(modelPath.FullName);

        if (splitModel)
        {
            // Save the tables to separate files in a subfolder called "tables".
            var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
            Directory.CreateDirectory(tablesFolderPath.FullName);

            foreach (var table in Tables)
            {
                await table.SaveModelAsync(tablesFolderPath);
            }

            // Save the views to separate files in a subfolder called "views".
            var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
            Directory.CreateDirectory(viewsFolderPath.FullName);

            foreach (var view in Views)
            {
                await view.SaveModelAsync(viewsFolderPath);
            }

            // Save the stored procedures to separate files in a subfolder called "storedprocedures".
            var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
            Directory.CreateDirectory(storedProceduresFolderPath.FullName);

            foreach (var storedProcedure in StoredProcedures)
            {
                await storedProcedure.SaveModelAsync(storedProceduresFolderPath);
            }

            // Add custom converters for the tables, views, and stored procedures
            // to only serialize the name, schema and relative path of the entity.
            _jsonSerializerOptions.Converters.Add(new SemanticModelTableJsonConverter());
            _jsonSerializerOptions.Converters.Add(new SemanticModelViewJsonConverter());
            _jsonSerializerOptions.Converters.Add(new SemanticModelStoredProcedureJsonConverter());
        }
        
        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        await File.WriteAllTextAsync(semanticModelJsonPath, JsonSerializer.Serialize(this, _jsonSerializerOptions));
    }

    /// <summary>
    /// Loads the semantic model from the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns>The loaded semantic model.</returns>
    public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        if (!File.Exists(semanticModelJsonPath))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelJsonPath);
        }

        await using var stream = File.OpenRead(semanticModelJsonPath);
        return await JsonSerializer.DeserializeAsync<SemanticModel>(stream, _jsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");
    }
}
