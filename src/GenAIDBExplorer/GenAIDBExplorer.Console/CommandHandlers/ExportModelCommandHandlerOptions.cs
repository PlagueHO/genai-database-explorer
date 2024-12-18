namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Export Model command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExportModelCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
/// <param name="outputFileName">The name of the output file. Defaults to "exported_model.md" if not supplied.</param>
/// <param name="fileType">The type of the output files. Defaults to "markdown".</param>
/// <param name="splitFiles">Flag to split the export into individual files per entity.</param>
public class ExportModelCommandHandlerOptions(
    DirectoryInfo projectPath,
    string? outputPath = null,
    string fileType = "markdown",
    bool splitFiles = false
) : CommandHandlerOptions(projectPath)
{
    public string OutputPath { get; } = outputPath ?? "";
    public string FileType { get; } = fileType;
    public bool SplitFiles { get; } = splitFiles;
}
