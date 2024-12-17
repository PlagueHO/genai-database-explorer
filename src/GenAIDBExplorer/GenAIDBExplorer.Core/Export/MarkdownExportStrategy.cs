namespace GenAIDBExplorer.Core.Export;

using System;
using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Export strategy for exporting the semantic model to Markdown format.
/// </summary>
public class MarkdownExportStrategy : IExportStrategy
{
    public async Task ExportAsync(SemanticModel semanticModel, ExportOptions options)
    {
        if (options.SplitFiles)
        {
            await ExportAsSeparateFilesAsync(semanticModel, options).ConfigureAwait(false);
        }
        else
        {
            await ExportAsSingleFileAsync(semanticModel, options).ConfigureAwait(false);
        }
    }

    private static async Task ExportAsSingleFileAsync(SemanticModel semanticModel, ExportOptions options)
    {
        var visitor = new MarkdownExportVisitor();
        semanticModel.Accept(visitor);
        var markdownContent = visitor.GetResult();

        var outputFileName = options.OutputFileName ?? "exported_model.md";
        var outputFilePath = Path.Combine(options.ProjectPath.FullName, outputFileName);

        await File.WriteAllTextAsync(outputFilePath, markdownContent).ConfigureAwait(false);
    }

    private static async Task ExportAsSeparateFilesAsync(SemanticModel semanticModel, ExportOptions options)
    {
        var outputDirectory = new DirectoryInfo(Path.Combine(options.ProjectPath.FullName, "ExportedModel"));

        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }

        var visitor = new MarkdownExportVisitor(outputDirectory);
        semanticModel.Accept(visitor);
        await visitor.SaveFilesAsync().ConfigureAwait(false);
    }
}
