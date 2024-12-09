// File: GenAIDBExplorer.Console/CommandHandlers/DataDictionaryCommandHandler.cs

using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.DataDictionaryProviders;
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
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<DataDictionaryCommandHandlerOptions>> logger
) : CommandHandler<DataDictionaryCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(DataDictionaryCommandHandler).Assembly);
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

        var sourcePathOption = new Option<DirectoryInfo>(
            aliases: ["--source-path", "-d"],
            description: "The path to the folder containing data dictionary files."
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

        // Create the base 'data-dictionary' command
        var dataDictionaryCommand = new Command("data-dictionary", "Process data dictionary files and update the semantic model.")
        {
            projectPathOption
        };

        // Create subcommands
        var tableCommand = new Command("table", "Process table data dictionary files.")
        {
            projectPathOption,
            sourcePathOption,
            schemaNameOption,
            nameOption,
            showOption
        };
        tableCommand.SetHandler(async (DirectoryInfo projectPath, DirectoryInfo sourcePath, string schemaName, string name, bool show) =>
        {
            var handler = host.Services.GetRequiredService<DataDictionaryCommandHandler>();
            var options = new DataDictionaryCommandHandlerOptions(
                projectPath,
                sourcePath,
                objectType: "table",
                schemaName: schemaName,
                objectName: name,
                show: show
            );
            await handler.HandleAsync(options);
        }, projectPathOption, sourcePathOption, schemaNameOption, nameOption, showOption);

        // Add subcommands to the 'data-dictionary' command
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
        var sourcePath = commandOptions.SourcePath;

        ValidateProjectPath(projectPath);

        if (!sourcePath.Exists)
        {
            _logger.LogError("The source path '{SourcePath}' does not exist.", sourcePath.FullName);
            return;
        }

        var semanticModel = await LoadSemanticModelAsync(projectPath);

        if (!string.IsNullOrEmpty(commandOptions.ObjectType))
        {
            switch (commandOptions.ObjectType.ToLower())
            {
                case "table":
                    await ProcessTableDataDictionaryAsync(semanticModel, sourcePath, commandOptions.SchemaName, commandOptions.ObjectName);
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

        // Save the updated semantic model
        _logger.LogInformation("Saving semantic model to '{ProjectPath}'", projectPath.FullName);
        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        await semanticModel.SaveModelAsync(semanticModelDirectory);
        _logger.LogInformation("Semantic model saved to '{ProjectPath}'", projectPath.FullName);

        _logger.LogInformation("Data dictionary processing complete for project '{ProjectPath}'", projectPath.FullName);
    }

    /// <summary>
    /// Processes table data dictionary files and updates the semantic model.
    /// </summary>
    /// <param name="semanticModel">The semantic model to update.</param>
    /// <param name="sourcePath">The directory containing data dictionary files.</param>
    /// <param name="schemaName">The schema name to filter tables.</param>
    /// <param name="tableName">The table name to filter tables.</param>
    private async Task ProcessTableDataDictionaryAsync(SemanticModel semanticModel, DirectoryInfo sourcePath, string? schemaName, string? tableName)
    {
        // Get list of markdown files from sourcePath
        var markdownFiles = Directory.GetFiles(sourcePath.FullName, "*.md", SearchOption.AllDirectories);

        if (markdownFiles.Length == 0)
        {
            _logger.LogWarning("No markdown files found in '{SourcePath}'", sourcePath.FullName);
            return;
        }

        var tables = await _dataDictionaryProvider.GetTablesFromMarkdownFilesAsync(markdownFiles);

        foreach (var table in tables)
        {
            if ((!string.IsNullOrEmpty(schemaName) && !table.Schema.Equals(schemaName, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(tableName) && !table.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Update or add the table in the semantic model
            var existingTable = semanticModel.FindTable(table.Schema, table.Name);
            if (existingTable != null)
            {
                _logger.LogInformation("Updating table '{Schema}.{Table}' in semantic model.", table.Schema, table.Name);
                // Update existing table
                existingTable.Description = table.Description;
                existingTable.Details = table.Details;
                existingTable.AdditionalInformation = table.AdditionalInformation;
                // existingTable.Columns = table.Columns;
                // existingTable.Indexes = table.Indexes;
            }
            else
            {
                _logger.LogInformation("Adding new table '{Schema}.{Table}' to semantic model.", table.Schema, table.Name);
                // Add new table
                semanticModel.AddTable(table);
            }
        }
    }
}