using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;
/// <summary>
/// Command handler for enriching the model for a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EnrichModelCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to enrich the model for.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class EnrichModelCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<EnrichModelCommandHandlerOptions>> logger
) : CommandHandler<EnrichModelCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(EnrichModelCommandHandler).Assembly);
    private readonly ISemanticDescriptionProvider _semanticDescriptionProvider = serviceProvider.GetRequiredService<ISemanticDescriptionProvider>();

    /// <summary>
    /// Sets up the enrich-model command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The enrich-model command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>(
            aliases: ["--project", "-p"],
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        var skipTablesOption = new Option<bool>(
            aliases: ["--skipTables"],
            description: "Flag to skip tables during the semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var skipViewsOption = new Option<bool>(
            aliases: ["--skipViews"],
            description: "Flag to skip views during the semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var skipStoredProceduresOption = new Option<bool>(
            aliases: ["--skipStoredProcedures"],
            description: "Flag to skip stored procedures during the semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var schemaNameOption = new Option<string>(
            aliases: ["--schema", "-s"],
            description: "The schema name of the object to enrich."
        )
        {
            ArgumentHelpName = "schemaName"
        };

        var nameOption = new Option<string>(
            aliases: ["--name", "-n"],
            description: "The name of the object to enrich."
        )
        {
            ArgumentHelpName = "name"
        };

        var showOption = new Option<bool>(
            aliases: ["--show"],
            description: "Display the entity after enrichment.",
            getDefaultValue: () => false
        );

        // Create the base 'enrich-model' command
        var enrichModelCommand = new Command("enrich-model", "Enrich an existing semantic model with descriptions in a GenAI Database Explorer project.")
        {
            projectPathOption,
            skipTablesOption,
            skipViewsOption,
            skipStoredProceduresOption
        };

        // Create subcommands
        var tableCommand = new Command("table", "Enrich a specific table.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            showOption
        };
        tableCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool show) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: false,
                skipViews: true,
                skipStoredProcedures: true,
                objectType: "table",
                schemaName,
                objectName: name,
                show: show
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, showOption);

        var viewCommand = new Command("view", "Enrich a specific view.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            showOption
        };
        viewCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool show) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: true,
                skipViews: false,
                skipStoredProcedures: true,
                objectType: "view",
                schemaName,
                objectName: name,
                show: show
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, showOption);

        var storedProcedureCommand = new Command("storedprocedure", "Enrich a specific stored procedure.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            showOption
        };
        storedProcedureCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool show) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: true,
                skipViews: true,
                skipStoredProcedures: false,
                objectType: "storedprocedure",
                schemaName,
                objectName: name,
                show: show
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, showOption);

        // Add subcommands to the 'enrich-model' command
        enrichModelCommand.AddCommand(tableCommand);
        enrichModelCommand.AddCommand(viewCommand);
        enrichModelCommand.AddCommand(storedProcedureCommand);

        // Set default handler if no subcommand is provided
        enrichModelCommand.SetHandler(async (DirectoryInfo projectPath, bool skipTables, bool skipViews, bool skipStoredProcedures) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(projectPath, skipTables, skipViews, skipStoredProcedures);
            await handler.HandleAsync(options);
        }, projectPathOption, skipTablesOption, skipViewsOption, skipStoredProceduresOption);

        return enrichModelCommand;
    }

    /// <summary>
    /// Handles the enrich-model command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(EnrichModelCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;
        var semanticModel = await LoadSemanticModelAsync(projectPath);

        if (!string.IsNullOrEmpty(commandOptions.ObjectType))
        {
            // Enrich specific object
            switch (commandOptions.ObjectType.ToLower())
            {
                case "table":
                    await EnrichTableAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    if (commandOptions.Show)
                    {
                        await ShowTableDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    }
                    break;
                case "view":
                    await EnrichViewAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    if (commandOptions.Show)
                    {
                        await ShowViewDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    }
                    break;
                case "storedprocedure":
                    await EnrichStoredProcedureAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    if (commandOptions.Show)
                    {
                        await ShowStoredProcedureDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    }
                    break;
                default:
                    _logger.LogError("{Message}", _resourceManagerLogMessages.GetString("InvalidObjectType"));
                    break;
            }
        }
        else
        {
            // Enrich all objects
            await EnrichAllObjectsAsync(semanticModel, commandOptions);
        }

        // Save the semantic model
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavingSemanticModel"), projectPath.FullName);
        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        await semanticModel.SaveModelAsync(semanticModelDirectory);
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavedSemanticModel"), projectPath.FullName);

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("EnrichSemanticModelComplete"), projectPath.FullName);
    }

    /// <summary>
    /// Enriches all objects in the semantic model.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="commandOptions"></param>
    /// <returns></returns>
    private async Task EnrichAllObjectsAsync(SemanticModel semanticModel, EnrichModelCommandHandlerOptions commandOptions)
    {
        if (!commandOptions.SkipTables)
        {
            await _semanticDescriptionProvider.UpdateTableSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }

        if (!commandOptions.SkipViews)
        {
            await _semanticDescriptionProvider.UpdateViewSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }

        if (!commandOptions.SkipStoredProcedures)
        {
            await _semanticDescriptionProvider.UpdateStoredProcedureSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Enriches the table with the specified schema name and table name.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="schemaName"></param>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private async Task EnrichTableAsync(SemanticModel semanticModel, string schemaName, string tableName)
    {
        var table = semanticModel.FindTable(schemaName, tableName);
        if (table == null)
        {
            _logger.LogError("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("TableNotFound"), schemaName, tableName);
            return;
        }

        await _semanticDescriptionProvider.UpdateTableSemanticDescriptionAsync(semanticModel, table).ConfigureAwait(false);
        _logger.LogInformation("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("EnrichedTable"), schemaName, tableName);
    }

    /// <summary>
    /// Enriches the view with the specified schema name and view name.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="schemaName"></param>
    /// <param name="viewName"></param>
    /// <returns></returns>
    private async Task EnrichViewAsync(SemanticModel semanticModel, string schemaName, string viewName)
    {
        var view = semanticModel.FindView(schemaName, viewName);
        if (view == null)
        {
            _logger.LogError("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("ViewNotFound"), schemaName, viewName);
            return;
        }

        await _semanticDescriptionProvider.UpdateViewSemanticDescriptionAsync(semanticModel, view).ConfigureAwait(false);
        _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("EnrichedView"), schemaName, viewName);
    }

    /// <summary>
    /// Enriches the stored procedure with the specified schema name and stored procedure name.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="schemaName"></param>
    /// <param name="storedProcedureName"></param>
    /// <returns></returns>
    private async Task EnrichStoredProcedureAsync(SemanticModel semanticModel, string schemaName, string storedProcedureName)
    {
        var storedProcedure = semanticModel.FindStoredProcedure(schemaName, storedProcedureName);
        if (storedProcedure == null)
        {
            _logger.LogError("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("StoredProcedureNotFound"), schemaName, storedProcedureName);
            return;
        }

        await _semanticDescriptionProvider.UpdateStoredProcedureSemanticDescriptionAsync(semanticModel, storedProcedure).ConfigureAwait(false);
        _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("EnrichedStoredProcedure"), schemaName, storedProcedureName);
    }
}