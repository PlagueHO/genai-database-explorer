using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

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
    /// Gets or sets additional information about the stored procedure.
    /// This is usually obtained from the data dictionary.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string AdditionalInformation { get; set; } = string.Empty;

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
        NotUsed = storedProcedure.NotUsed;
        NotUsedReason = storedProcedure.NotUsedReason;
        Parameters = storedProcedure.Parameters;
        Definition = storedProcedure.Definition;
    }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        return new DirectoryInfo(Path.Combine("storedprocedures", GetModelEntityFilename().Name));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(base.ToString());

        if (!string.IsNullOrWhiteSpace(Parameters))
        {
            builder.AppendLine("");
            builder.AppendLine("Parameters:");
            builder.AppendLine(Parameters);
        }

        if (!string.IsNullOrWhiteSpace(Definition))
        {
            builder.AppendLine("");
            builder.AppendLine("Definition:");
            builder.AppendLine(Definition);
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.AppendLine("");
            builder.AppendLine("Description:");
            builder.AppendLine(Description);
        }

        if (!string.IsNullOrWhiteSpace(SemanticDescription))
        {
            builder.AppendLine("");
            builder.AppendLine("Semantic Description:");
            builder.AppendLine(SemanticDescription);
        }

        return builder.ToString();
    }

    /// <inheritdoc/>
    public override void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitStoredProcedure(this);
    }
}