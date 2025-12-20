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
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project.",
            Required = true
        };

        var sourcePathOption = new Option<string>("--source-path", "-d")
        {
            Description = "The path to the source directory containing data dictionary files. Supports file masks.",
            Required = true
        };

        var schemaNameOption = new Option<string>("--schema-name", "-s")
        {
            Description = "The schema name of the object to process."
        };

        var nameOption = new Option<string>("--name", "-n")
        {
            Description = "The name of the object to process.",
            HelpName = "name"
        };

        var showOption = new Option<bool>("--show")
        {
            Description = "Display the entity after processing."
        };

        var dataDictionaryCommand = new Command("data-dictionary", "Process data dictionary files and update the semantic model.");
        dataDictionaryCommand.Options.Add(projectPathOption);

        var tableCommand = new Command("table", "Process table data dictionary files.");
        tableCommand.Options.Add(projectPathOption);
        tableCommand.Options.Add(sourcePathOption);
        tableCommand.Options.Add(schemaNameOption);
        tableCommand.Options.Add(nameOption);
        tableCommand.Options.Add(showOption);

        tableCommand.SetAction(async (parseResult) =>
        {
            var projectPath = parseResult.GetValue(projectPathOption)!;
            var sourcePathPattern = parseResult.GetValue(sourcePathOption)!;
            var schemaName = parseResult.GetValue(schemaNameOption);
            var name = parseResult.GetValue(nameOption);
            var show = parseResult.GetValue(showOption);

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
        });

        dataDictionaryCommand.Subcommands.Add(tableCommand);

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

                    try
                    {
                        await _dataDictionaryProvider.EnrichSemanticModelFromDataDictionaryAsync(
                            semanticModel,
                            sourcePathPattern,
                            commandOptions.SchemaName,
                            commandOptions.ObjectName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enrich semantic model from data dictionary at '{SourcePath}'. Continuing.", sourcePathPattern);
                    }

                    if (commandOptions.Show)
                    {
                        try
                        {
                            if (commandOptions.SchemaName != null && commandOptions.ObjectName != null)
                            {
                                await ShowTableDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                            }
                            else
                            {
                                _logger.LogWarning("Cannot show table details: SchemaName or ObjectName is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to display table details after enrichment");
                        }
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
        await _semanticModelProvider.SaveSemanticModelAsync(semanticModel).ConfigureAwait(false);
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavedSemanticModel"), projectPath.FullName);

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("DataDictionaryProcessingComplete"), projectPath.FullName);
    }
}