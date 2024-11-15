using System.Text.Json;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a stored procedure in the semantic model.
/// </summary>
public sealed class SemanticModelStoredProcedure(
    string schema,
    string name,
    string definition,
    string? parameters = null,
    string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets or sets the parameters of the stored procedure.
    /// </summary>
    public string? Parameters { get; set; } = parameters;

    /// <summary>
    /// Gets or sets the definition of the stored procedure.
    /// </summary>
    public string Definition { get; set; } = definition;

    /// <inheritdoc/>
    public new async Task LoadModelAsync(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified stored procedure file does not exist.", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var storedProcedure = JsonSerializer.Deserialize<SemanticModelStoredProcedure>(json) ?? throw new InvalidOperationException("Failed to load stored procedure.");

        Schema = storedProcedure.Schema;
        Name = storedProcedure.Name;
        Description = storedProcedure.Description;
        SemanticDescription = storedProcedure.SemanticDescription;
        IsIgnored = storedProcedure.IsIgnored;
        Parameters = storedProcedure.Parameters;
        Definition = storedProcedure.Definition;
    }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        return new DirectoryInfo(Path.Combine("storedprocedures", GetModelEntityFilename().Name));
    }
}