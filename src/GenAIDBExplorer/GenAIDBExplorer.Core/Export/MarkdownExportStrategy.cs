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
        var (filePath, baseDirectory) = ResolveOutputPath(options.OutputPath, options.SplitFiles, options.ProjectPath);

        if (options.SplitFiles)
        {
            await ExportWithSplitFilesAsync(semanticModel, filePath, baseDirectory).ConfigureAwait(false);
        }
        else
        {
            await ExportSingleFileAsync(semanticModel, filePath).ConfigureAwait(false);
        }
    }

    private static (string filePath, string baseDirectory) ResolveOutputPath(string? outputPath, bool splitFiles, DirectoryInfo projectPath)
    {
        const string defaultFileName = "exported_model";
        const string fileExtension = ".md";
        string directory;
        string fileName;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            directory = projectPath.FullName;
            fileName = defaultFileName + fileExtension;
        }
        else if (IsDirectoryPath(outputPath))
        {
            directory = Path.GetFullPath(outputPath, projectPath.FullName);
            fileName = defaultFileName + fileExtension;
        }
        else
        {
            fileName = Path.GetFileName(outputPath);
            var specifiedDirectory = Path.GetDirectoryName(outputPath);
            directory = specifiedDirectory != null
                ? Path.GetFullPath(specifiedDirectory, projectPath.FullName)
                : projectPath.FullName;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = defaultFileName + fileExtension;
            }
            else if (!Path.HasExtension(fileName))
            {
                fileName += fileExtension;
            }
        }

        string filePath = Path.Combine(directory, fileName);
        return (filePath, directory);
    }

    private static bool IsDirectoryPath(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
    }

    private static async Task ExportSingleFileAsync(SemanticModel semanticModel, string filePath)
    {
        var visitor = new MarkdownExportVisitor();
        semanticModel.Accept(visitor);
        var markdownContent = visitor.GetResult();

        await File.WriteAllTextAsync(filePath, markdownContent).ConfigureAwait(false);
    }

    private static async Task ExportWithSplitFilesAsync(SemanticModel semanticModel, string filePath, string baseDirectory)
    {
        var outputDirectory = new DirectoryInfo(baseDirectory);

        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }

        var visitor = new MarkdownExportVisitor(outputDirectory);
        semanticModel.Accept(visitor);
        await visitor.SaveFilesAsync().ConfigureAwait(false);

        // Save the main file
        var mainFileContent = visitor.GetResult();
        await File.WriteAllTextAsync(filePath, mainFileContent).ConfigureAwait(false);
    }
}
