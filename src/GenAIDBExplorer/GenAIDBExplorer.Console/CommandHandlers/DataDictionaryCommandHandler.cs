// File: GenAIDBExplorer.Console/CommandHandlers/DataDictionaryCommandHandler.cs

using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.DataDictionary;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for processing data dictionary files and updating the semantic model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DataDictionaryCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to update.</param>
/// <param name="connectionProvider">The database connection provider instance.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
/// <param name="dataDictionaryProvider">The data dictionary provider instance.</param>
public class DataDictionaryCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<DataDictionaryCommandHandlerOptions>> logger
) : CommandHandler<DataDictionaryCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(DataDictionaryCommandHandler).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Console.Resources.ErrorMessages", typeof(DataDictionaryCommandHandler).Assembly);
    private readonly IDataDictionaryProvider _dataDictionaryProvider = serviceProvider.GetRequiredService<IDataDictionaryProvider>();

    /// <summary>
    /// Sets up the data-dictionary command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The data-dictionary command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>(
            aliases: ["--project", "-p"],
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        var sourcePathOption = new Option<string>(
            aliases: ["--source-path", "-d"],
            description: "The path to the source directory containing data dictionary files. Supports file masks."
        )
        {
            IsRequired = true
        };

        var schemaNameOption = new Option<string>(
            aliases: ["--schema", "-s"],
            description: "The schema name of the object to process."
        )
        {
            ArgumentHelpName = "schemaName"
        };

        var nameOption = new Option<string>(
            aliases: ["--name", "-n"],
            description: "The name of the object to process."
        )
        {
            ArgumentHelpName = "name"
        };

        var showOption = new Option<bool>(
            aliases: ["--show"],
            description: "Display the entity after processing.",
            getDefaultValue: () => false
        );

        var dataDictionaryCommand = new Command("data-dictionary", "Process data dictionary files and update the semantic model.")
        {
            projectPathOption
        };

        var tableCommand = new Command("table", "Process table data dictionary files.")
        {
            projectPathOption,
            sourcePathOption,
            schemaNameOption,
            nameOption,
            showOption
        };
        tableCommand.SetHandler(async (DirectoryInfo projectPath, string sourcePathPattern, string schemaName, string name, bool show) =>
        {
            var handler = host.Services.GetRequiredService<DataDictionaryCommandHandler>();
            var options = new DataDictionaryCommandHandlerOptions(
                projectPath,
                sourcePathPattern,
                objectType: "table",
                schemaName: schemaName,
                objectName: name,
                show: show
            );
            await handler.HandleAsync(options);
        }, projectPathOption, sourcePathOption, schemaNameOption, nameOption, showOption);

        dataDictionaryCommand.AddCommand(tableCommand);

        return dataDictionaryCommand;
    }

    /// <summary>
    /// Handles the data-dictionary command with the specified options.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(DataDictionaryCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;
        var sourcePathPattern = commandOptions.SourcePathPattern;

        ValidateProjectPath(projectPath);

        var directory = Path.GetDirectoryName(sourcePathPattern);

        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        if (!Directory.Exists(directory))
        {
            _logger.LogError("{ErrorMessage} '{SourcePath}'", _resourceManagerErrorMessages.GetString("DataDictionarySourcePathDoesNotExist"), directory);
            return;
        }

        var semanticModel = await LoadSemanticModelAsync(projectPath);

        if (!string.IsNullOrEmpty(commandOptions.ObjectType))
        {
            switch (commandOptions.ObjectType.ToLower())
            {
                case "table":

                    await _dataDictionaryProvider.EnrichSemanticModelFromDataDictionaryAsync(
                        semanticModel,
                        sourcePathPattern,
                        commandOptions.SchemaName,
                        commandOptions.ObjectName);
                    if (commandOptions.Show)
                    {
                        await ShowTableDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    }
                    break;
                default:
                    _logger.LogError("Invalid object type specified: {ObjectType}", commandOptions.ObjectType);
                    break;
            }
        }
        else
        {
            _logger.LogError("No object type specified.");
        }

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavingSemanticModel"), projectPath.FullName);
        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        await semanticModel.SaveModelAsync(semanticModelDirectory);
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavedSemanticModel"), projectPath.FullName);

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("DataDictionaryProcessingComplete"), projectPath.FullName);
    }
}