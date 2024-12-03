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
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class EnrichModelCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<EnrichModelCommandHandlerOptions>> logger
) : CommandHandler<EnrichModelCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(EnrichModelCommandHandler).Assembly);

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

        var singleModelFileOption = new Option<bool>(
            aliases: ["--singleModelFile"],
            description: "Flag to save the semantic model as a single file.",
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

        // Create the base 'enrich-model' command
        var enrichModelCommand = new Command("enrich-model", "Enrich an existing semantic model with descriptions in a GenAI Database Explorer project.")
        {
            projectPathOption,
            skipTablesOption,
            skipViewsOption,
            skipStoredProceduresOption,
            singleModelFileOption
        };

        // Create subcommands
        var tableCommand = new Command("table", "Enrich a specific table.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            singleModelFileOption
        };
        tableCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: false,
                skipViews: true,
                skipStoredProcedures: true,
                singleModelFile,
                objectType: "table",
                schemaName,
                objectName: name
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, singleModelFileOption);

        var viewCommand = new Command("view", "Enrich a specific view.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            singleModelFileOption
        };
        viewCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: true,
                skipViews: false,
                skipStoredProcedures: true,
                singleModelFile,
                objectType: "view",
                schemaName,
                objectName: name
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, singleModelFileOption);

        var storedProcedureCommand = new Command("storedprocedure", "Enrich a specific stored procedure.")
        {
            projectPathOption,
            schemaNameOption,
            nameOption,
            singleModelFileOption
        };
        storedProcedureCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(
                projectPath,
                skipTables: true,
                skipViews: true,
                skipStoredProcedures: false,
                singleModelFile,
                objectType: "storedprocedure",
                schemaName,
                objectName: name
            );
            await handler.HandleAsync(options);
        }, projectPathOption, schemaNameOption, nameOption, singleModelFileOption);

        // Add subcommands to the 'enrich-model' command
        enrichModelCommand.AddCommand(tableCommand);
        enrichModelCommand.AddCommand(viewCommand);
        enrichModelCommand.AddCommand(storedProcedureCommand);

        // Set default handler if no subcommand is provided
        enrichModelCommand.SetHandler(async (DirectoryInfo projectPath, bool skipTables, bool skipViews, bool skipStoredProcedures, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<EnrichModelCommandHandler>();
            var options = new EnrichModelCommandHandlerOptions(projectPath, skipTables, skipViews, skipStoredProcedures, singleModelFile);
            await handler.HandleAsync(options);
        }, projectPathOption, skipTablesOption, skipViewsOption, skipStoredProceduresOption, singleModelFileOption);

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
                    break;
                case "view":
                    await EnrichViewAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                case "storedprocedure":
                    await EnrichStoredProcedureAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                default:
                    _logger.LogError("{Message}", _resourceManagerLogMessages.GetString("InvalidObjectType"));
                    break;
            }
        }
        else
        {
            // Enrich all objects
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

        // Save the semantic model
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavingSemanticModel"), projectPath.FullName);
        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        await semanticModel.SaveModelAsync(semanticModelDirectory, !commandOptions.SingleModelFile);
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("SavedSemanticModel"), projectPath.FullName);

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("EnrichSemanticModelComplete"), projectPath.FullName);
    }

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