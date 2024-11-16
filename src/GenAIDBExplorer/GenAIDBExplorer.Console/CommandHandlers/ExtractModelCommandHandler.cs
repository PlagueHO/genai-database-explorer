﻿using GenAIDBExplorer.AI.SemanticProviders;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for extracting a model from a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExtractModelCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to extract the model from.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class ExtractModelCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<ExtractModelCommandHandlerOptions>> logger
) : CommandHandler<ExtractModelCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(ExtractModelCommandHandler).Assembly);

    /// <summary>
    /// Sets up the extract model command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The extract model command.</returns>
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
            description: "Flag to skip tables during the extract model process.",
            getDefaultValue: () => false
        );

        var skipViewsOption = new Option<bool>(
            aliases: ["--skipViews"],
            description: "Flag to skip views during the extract model process.",
            getDefaultValue: () => false
        );

        var skipStoredProceduresOption = new Option<bool>(
            aliases: ["--skipStoredProcedures"],
            description: "Flag to skip stored procedures during the extract model process.",
            getDefaultValue: () => false
        );

        var singleModelFileOption = new Option<bool>(
            aliases: ["--singleModelFile"],
            description: "Flag to produce the model as a single file.",
            getDefaultValue: () => false
        );

        var extractModelCommand = new Command("extract-model", "Extract a semantic model from a SQL database for a GenAI Database Explorer project.");
        extractModelCommand.AddOption(projectPathOption);
        extractModelCommand.AddOption(skipTablesOption);
        extractModelCommand.AddOption(skipViewsOption);
        extractModelCommand.AddOption(skipStoredProceduresOption);
        extractModelCommand.AddOption(singleModelFileOption);
        extractModelCommand.SetHandler(async (DirectoryInfo projectPath, bool skipTables, bool skipViews, bool skipStoredProcedures, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<ExtractModelCommandHandler>();
            var options = new ExtractModelCommandHandlerOptions(projectPath, skipTables, skipViews, skipStoredProcedures, singleModelFile);
            await handler.HandleAsync(options);
        }, projectPathOption, skipTablesOption, skipViewsOption, skipStoredProceduresOption, singleModelFileOption);

        return extractModelCommand;
    }

    /// <summary>
    /// Handles the extract model command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(ExtractModelCommandHandlerOptions commandOptions)
    {
        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation(_resourceManagerLogMessages.GetString("ExtractingSemanticModel"), projectPath.FullName);

        _project.LoadConfiguration(projectPath.FullName);

        // Extract the Semantic Model
        var semanticModel = await _semanticModelProvider.ExtractSemanticModelAsync().ConfigureAwait(false);

        // Save the Semantic Model into the project directory into a subdirectory with the name of the semanticModel.Name
        var semanticModelDirectory = new DirectoryInfo(Path.Combine(projectPath.FullName, semanticModel.Name));
        if (!semanticModelDirectory.Exists)
        {
            semanticModelDirectory.Create();
        }

        _logger.LogInformation(_resourceManagerLogMessages.GetString("SavingSemanticModel"), semanticModelDirectory);
        await semanticModel.SaveModelAsync(semanticModelDirectory, !commandOptions.SingleModelFile);

        _logger.LogInformation(_resourceManagerLogMessages.GetString("ExtractSemanticModelComplete"), projectPath.FullName);
    }
}