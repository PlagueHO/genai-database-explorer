using System.Text.Json;
using System.Text.Json.Serialization;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel.JsonConverters;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model for a database.
/// </summary>
public sealed class SemanticModel(
    string name,
    string source,
    string? description = null
    ) : ISemanticModel
{
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
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model will be saved.</param>
    public async Task SaveModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        // Save the semantic model to a JSON file.
        Directory.CreateDirectory(modelPath.FullName);

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
        jsonSerializerOptions.Converters.Add(new SemanticModelTableJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelViewJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelStoredProcedureJsonConverter());

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        await File.WriteAllTextAsync(semanticModelJsonPath, JsonSerializer.Serialize(this, jsonSerializerOptions));
    }

    /// <summary>
    /// Loads the semantic model from the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns>The loaded semantic model.</returns>
    public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        if (!File.Exists(semanticModelJsonPath))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelJsonPath);
        }

        await using var stream = File.OpenRead(semanticModelJsonPath);
        var semanticModel = await JsonSerializer.DeserializeAsync<SemanticModel>(stream, jsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");

        // Load the tables listed in the model from the files in the "tables" subfolder.
        var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
        if (Directory.Exists(tablesFolderPath.FullName))
        {
            foreach (var table in semanticModel.Tables)
            {
                await table.LoadModelAsync(tablesFolderPath);
            }
        }

        // Load the views listed in the model from the files in the "views" subfolder.
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        if (Directory.Exists(viewsFolderPath.FullName))
        {
            foreach (var view in semanticModel.Views)
            {
                await view.LoadModelAsync(viewsFolderPath);
            }
        }

        // Load the stored procedures listed in the model from the files in the "storedprocedures" subfolder.
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        if (Directory.Exists(storedProceduresFolderPath.FullName))
        {
            foreach (var storedProcedure in semanticModel.StoredProcedures)
            {
                await storedProcedure.LoadModelAsync(storedProceduresFolderPath);
            }
        }

        return semanticModel;
    }

    /// <summary>
    /// Gets the tables in the semantic model.
    /// </summary>
    public List<SemanticModelTable> Tables { get; set; } = [];

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
    /// Finds a table in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The table if found; otherwise, null.</returns>
    public SemanticModelTable? FindTable(string schemaName, string tableName)
    {
        return Tables.FirstOrDefault(t => t.Schema == schemaName && t.Name == tableName);
    }

    /// <summary>
    /// Selects tables from the semantic model that match the schema and table names in the provided TableList.
    /// </summary>
    /// <param name="tableList">The list of tables to match.</param>
    /// <returns>A list of matching SemanticModelTable objects.</returns>
    public List<SemanticModelTable> SelectTables(TableList tableList)
    {
        var selectedTables = new List<SemanticModelTable>();

        foreach (var tableInfo in tableList.Tables)
        {
            var matchingTable = Tables.FirstOrDefault(t => t.Schema == tableInfo.SchemaName && t.Name == tableInfo.TableName);
            if (matchingTable != null)
            {
                selectedTables.Add(matchingTable);
            }
        }

        return selectedTables;
    }

    /// <summary>
    /// Gets the views in the semantic model.
    /// </summary>
    public List<SemanticModelView> Views { get; set; } = [];

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
    /// Finds a view in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The view if found; otherwise, null.</returns></returns>
    public SemanticModelView? FindView(string schemaName, string viewName)
    {
        return Views.FirstOrDefault(v => v.Schema == schemaName && v.Name == viewName);
    }

    /// <summary>
    /// Gets the stored procedures in the semantic model.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures { get; set; } = [];

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
    /// Finds a stored procedure in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The stored procedure if found; otherwise, null.</returns>
    public SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName)
    {
        return StoredProcedures.FirstOrDefault(sp => sp.Schema == schemaName && sp.Name == storedProcedureName);
    }

    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    public void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitSemanticModel(this);
        foreach (var table in Tables)
        {
            table.Accept(visitor);
        }

        foreach (var view in Views)
        {
            view.Accept(visitor);
        }

        foreach (var storedProcedure in StoredProcedures)
        {
            storedProcedure.Accept(visitor);
        }
    }

}