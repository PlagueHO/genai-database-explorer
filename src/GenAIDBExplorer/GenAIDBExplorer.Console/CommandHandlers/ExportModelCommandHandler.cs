using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Export;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Console.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for exporting a semantic model from a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExportModelCommandHandler"/> class.
/// </remarks>
public class ExportModelCommandHandler : CommandHandler<ExportModelCommandHandlerOptions>
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(ExportModelCommandHandler).Assembly);

    // Define available export strategies
    private readonly Dictionary<string, IExportStrategy> _exportStrategies;

    public ExportModelCommandHandler(
        IProject project,
        IDatabaseConnectionProvider connectionProvider,
        ISemanticModelProvider semanticModelProvider,
        IOutputService outputService,
        IServiceProvider serviceProvider,
        ILogger<ICommandHandler<ExportModelCommandHandlerOptions>> logger
    ) : base(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
    {
        _exportStrategies = new Dictionary<string, IExportStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            { "markdown", new MarkdownExportStrategy() }
            // Future strategies can be added here
        };
    }

    /// <summary>
    /// Sets up the export-model command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The export-model command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project.",
            Required = true
        };

        var outputFileNameOption = new Option<string?>("--outputFileName", "-o")
        {
            Description = "The path to the output file."
        };

        var fileTypeOption = new Option<string>("--fileType", "-f")
        {
            Description = "The type of the output files. Defaults to 'markdown'."
        };

        var splitFilesOption = new Option<bool>("--splitFiles", "-s")
        {
            Description = "Flag to split the export into individual files per entity."
        };

        var exportModelCommand = new Command("export-model", "Export the semantic model from a GenAI Database Explorer project.");
        exportModelCommand.Options.Add(projectPathOption);
        exportModelCommand.Options.Add(outputFileNameOption);
        exportModelCommand.Options.Add(fileTypeOption);
        exportModelCommand.Options.Add(splitFilesOption);

        exportModelCommand.SetAction(async (parseResult) =>
        {
            var projectPath = parseResult.GetValue(projectPathOption)!;
            var outputFileName = parseResult.GetValue(outputFileNameOption);
            var fileType = parseResult.GetValue(fileTypeOption) ?? "markdown";
            var splitFiles = parseResult.GetValue(splitFilesOption);
            
            var handler = host.Services.GetRequiredService<ExportModelCommandHandler>();
            var options = new ExportModelCommandHandlerOptions(projectPath, outputFileName, fileType, splitFiles);
            await handler.HandleAsync(options);
        });

        return exportModelCommand;
    }

    /// <summary>
    /// Handles the export-model command with the specified options.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(ExportModelCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("ExportingSemanticModel"), projectPath.FullName);

        _project.LoadProjectConfiguration(projectPath);

        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        var semanticModel = await _semanticModelProvider.LoadSemanticModelAsync(semanticModelDirectory).ConfigureAwait(false);

        if (_exportStrategies.TryGetValue(commandOptions.FileType, out var exportStrategy))
        {
            var exportOptions = new ExportOptions
            {
                ProjectPath = projectPath,
                OutputPath = commandOptions.OutputPath,
                SplitFiles = commandOptions.SplitFiles
            };

            await exportStrategy.ExportAsync(semanticModel, exportOptions).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"The file type '{commandOptions.FileType}' is not supported.");
        }

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("ExportSemanticModelComplete"), projectPath.FullName);
    }
}