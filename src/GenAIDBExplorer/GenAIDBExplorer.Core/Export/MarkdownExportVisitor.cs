namespace GenAIDBExplorer.Core.Export;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Visitor implementation for exporting semantic model entities to Markdown format.
/// </summary>
public class MarkdownExportVisitor(
    DirectoryInfo? outputDirectory = null
) : ISemanticModelVisitor
{
    private readonly StringBuilder _content = new();
    private readonly DirectoryInfo? _outputDirectory = outputDirectory;
    private readonly Dictionary<string, StringBuilder> _filesContent = new();

    public string GetResult() => _content.ToString();

    public async Task SaveFilesAsync()
    {
        if (_outputDirectory == null) return;

        foreach (var kvp in _filesContent)
        {
            var filePath = Path.Combine(_outputDirectory.FullName, kvp.Key);
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            await File.WriteAllTextAsync(filePath, kvp.Value.ToString()).ConfigureAwait(false);
        }
    }

    public void VisitSemanticModel(SemanticModel semanticModel)
    {
        if (_outputDirectory == null)
        {
            // Single file export
            _content.AppendLine($"# Semantic Model {semanticModel.Name}");
            _content.AppendLine();

            if (!string.IsNullOrWhiteSpace(semanticModel.Description))
            {
                _content.AppendLine(semanticModel.Description);
                _content.AppendLine();
            }

            // Tables, Views, and Stored Procedures will be visited individually.
        }
        else
        {
            // Multi-file export: Generate index file content
            var indexContent = new StringBuilder();
            indexContent.AppendLine($"# Semantic Model {semanticModel.Name}");
            indexContent.AppendLine();

            if (!string.IsNullOrWhiteSpace(semanticModel.Description))
            {
                indexContent.AppendLine(semanticModel.Description);
                indexContent.AppendLine();
            }

            indexContent.AppendLine();
            indexContent.AppendLine("## Tables");
            indexContent.AppendLine();
            foreach (var table in semanticModel.Tables.OrderBy(t => t.Schema).ThenBy(t => t.Name))
            {
                var fileName = $"tables/{table.Schema}.{table.Name}.md";
                indexContent.AppendLine($"- [{table.Schema}.{table.Name}]({fileName})");
            }

            indexContent.AppendLine();
            indexContent.AppendLine("## Views");
            indexContent.AppendLine();
            foreach (var view in semanticModel.Views.OrderBy(t => t.Schema).ThenBy(t => t.Name))
            {
                var fileName = $"views/{view.Schema}.{view.Name}.md";
                indexContent.AppendLine($"- [{view.Schema}.{view.Name}]({fileName})");
            }

            indexContent.AppendLine();
            indexContent.AppendLine("## Stored Procedures");
            indexContent.AppendLine();
            foreach (var sp in semanticModel.StoredProcedures.OrderBy(t => t.Schema).ThenBy(t => t.Name))
            {
                var fileName = $"storedprocedures/{sp.Schema}.{sp.Name}.md";
                indexContent.AppendLine($"- [{sp.Schema}.{sp.Name}]({fileName})");
            }

            _filesContent.Add("index.md", indexContent);
        }
    }

    public void VisitTable(SemanticModelTable table)
    {
        var content = new StringBuilder();
        content.AppendLine($"## Table [{table.Schema}].[{table.Name}]");
        content.AppendLine();

        if (!string.IsNullOrWhiteSpace(table.Description))
        {
            content.AppendLine(table.Description);
            content.AppendLine();
        }

        content.AppendLine("| Column Name | Data Type | Is Nullable | Is Primary Key | Is Identity | Is Computed | Is XML Document | Max Length | Precision | Scale | Description | Referenced Table | Referenced Column |");
        content.AppendLine("|-------------|-----------|-------------|----------------|-------------|-------------|-----------------|------------|-----------|-------|-------------|------------------|-------------------|");

        foreach (var column in table.Columns)
        {
            content.AppendLine($"| {column.Name} | {column.Type} | {column.IsNullable} | {column.IsPrimaryKey} | {column.IsIdentity} | {column.IsComputed} | {column.IsXmlDocument} | {column.MaxLength?.ToString() ?? ""} | {column.Precision?.ToString() ?? ""} | {column.Scale?.ToString() ?? ""} | {column.Description ?? ""} | {column.ReferencedTable ?? ""} | {column.ReferencedColumn ?? ""} |");
        }

        content.AppendLine();
        content.AppendLine("### Indexes");
        content.AppendLine("| Index Name | Column Name | Is Unique | Is Primary Key | Is Unique Constraint | Description |");
        content.AppendLine("|------------|-------------|-----------|----------------|----------------------|-------------|");

        foreach (var index in table.Indexes)
        {
            content.AppendLine($"| {index.Name} | {index.ColumnName ?? ""} | {index.IsUnique} | {index.IsPrimaryKey} | {index.IsUniqueConstraint} | {index.Description ?? ""} |");
        }

        if (_outputDirectory == null)
        {
            _content.Append(content);
            _content.AppendLine();
        }
        else
        {
            var fileName = $"tables/{table.Schema}.{table.Name}.md";
            _filesContent[fileName] = content;
        }
    }

    public void VisitView(SemanticModelView view)
    {
        var content = new StringBuilder();
        content.AppendLine($"## View [{view.Schema}].[{view.Name}]");
        content.AppendLine();

        if (!string.IsNullOrWhiteSpace(view.Description))
        {
            content.AppendLine(view.Description);
            content.AppendLine();
        }

        content.AppendLine("| Column Name | Data Type | Is Nullable | Is Primary Key | Is Identity | Is Computed | Is XML Document | Max Length | Precision | Scale | Description | Referenced Table | Referenced Column |");
        content.AppendLine("|-------------|-----------|-------------|----------------|-------------|-------------|-----------------|------------|-----------|-------|-------------|------------------|-------------------|");

        foreach (var column in view.Columns)
        {
            content.AppendLine($"| {column.Name} | {column.Type} | {column.IsNullable} | {column.IsPrimaryKey} | {column.IsIdentity} | {column.IsComputed} | {column.IsXmlDocument} | {column.MaxLength?.ToString() ?? ""} | {column.Precision?.ToString() ?? ""} | {column.Scale?.ToString() ?? ""} | {column.Description ?? ""} | {column.ReferencedTable ?? ""} | {column.ReferencedColumn ?? ""} |");
        }

        if (!string.IsNullOrWhiteSpace(view.Definition))
        {
            content.AppendLine("### Definition");
            content.AppendLine(view.Definition);
            content.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(view.AdditionalInformation))
        {
            content.AppendLine("### Additional Information");
            content.AppendLine(view.AdditionalInformation);
            content.AppendLine();
        }

        if (_outputDirectory == null)
        {
            _content.Append(content);
            _content.AppendLine();
        }
        else
        {
            var fileName = $"views/{view.Schema}.{view.Name}.md";
            _filesContent[fileName] = content;
        }
    }

    public void VisitStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        var content = new StringBuilder();
        content.AppendLine($"## Stored Procedure [{storedProcedure.Schema}].[{storedProcedure.Name}]");
        content.AppendLine();

        if (!string.IsNullOrWhiteSpace(storedProcedure.Description))
        {
            content.AppendLine(storedProcedure.Description);
            content.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(storedProcedure.Parameters))
        {
            content.AppendLine("### Parameters");
            content.AppendLine(storedProcedure.Parameters);
            content.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(storedProcedure.Definition))
        {
            content.AppendLine("### Definition");
            content.AppendLine(storedProcedure.Definition);
            content.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(storedProcedure.AdditionalInformation))
        {
            content.AppendLine("### Additional Information");
            content.AppendLine(storedProcedure.AdditionalInformation);
            content.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(storedProcedure.SemanticDescription))
        {
            content.AppendLine("### Semantic Description");
            content.AppendLine(storedProcedure.SemanticDescription);
            content.AppendLine();
        }

        if (_outputDirectory == null)
        {
            _content.Append(content);
            _content.AppendLine();
        }
        else
        {
            var fileName = $"storedprocedures/{storedProcedure.Schema}.{storedProcedure.Name}.md";
            _filesContent[fileName] = content;
        }
    }

    public void VisitColumn(SemanticModelColumn column)
    {
        // Not needed individually since columns are processed in their parent entities.
    }

    public void VisitIndex(SemanticModelIndex index)
    {
        // Not needed individually since indexes are processed in their parent entities.
    }
}
