using GenAIDBExplorer.AI.SemanticProviders;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

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
            description: "Flag to skip tables during the description semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var skipViewsOption = new Option<bool>(
            aliases: ["--skipViews"],
            description: "Flag to skip views during the description semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var skipStoredProceduresOption = new Option<bool>(
            aliases: ["--skipStoredProcedures"],
            description: "Flag to skip stored procedures during the semantic model enrichment process.",
            getDefaultValue: () => false
        );

        var enrichModelCommand = new Command("enrich-model", "Enrich an existing semantic model with descriptions in a GenAI Database Explorer project.");
        enrichModelCommand.AddOption(projectPathOption);
        enrichModelCommand.AddOption(skipTablesOption);
        enrichModelCommand.AddOption(skipViewsOption);
        enrichModelCommand.AddOption(skipStoredProceduresOption);
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
        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation(LogMessages.EnrichingSemanticModel, projectPath.FullName);

        _project.LoadConfiguration(projectPath.FullName);

        // Load the Semantic Model
        var semanticModelDirectory = new DirectoryInfo(Path.Combine(projectPath.FullName, _project.Settings.Database.Name));
        var semanticModel = await _semanticModelProvider.LoadSemanticModelAsync(semanticModelDirectory);

        if (!commandOptions.SkipTables)
        {
            // For each table generate the Semantic Description using the Semantic Description Provider
            foreach (var table in semanticModel.Tables)
            {
                await _semanticDescriptionProvider.UpdateSemanticDescriptionAsync(table).ConfigureAwait(false);
            }
        }

        if (!commandOptions.SkipViews)
        {
            // For each view generate the Semantic Description using the Semantic Description Provider
            foreach (var view in semanticModel.Views)
            {
                await _semanticDescriptionProvider.UpdateSemanticDescriptionAsync(view).ConfigureAwait(false);
            }
        }

        if (!commandOptions.SkipStoredProcedures)
        {
            // For each stored procedure generate the Semantic Description using the Semantic Description Provider
            foreach (var storedProcedure in semanticModel.StoredProcedures)
            {
                await _semanticDescriptionProvider.UpdateSemanticDescriptionAsync(storedProcedure).ConfigureAwait(false);
            }
        }

        // Save the semantic model
        _logger.LogInformation(LogMessages.SavingSemanticModel, projectPath.FullName);
        await semanticModel.SaveModelAsync(semanticModelDirectory);

        _logger.LogInformation(LogMessages.EnrichSemanticModelComplete, projectPath.FullName);
    }

    /// <summary>
    /// Contains log messages used in the <see cref="EnrichModelCommandHandler"/> class.
    /// </summary>
    public static class LogMessages
    {
        public const string EnrichingSemanticModel = "Enriching semantic model with descriptions for project at '{ProjectPath}'.";
        public const string SavingSemanticModel = "Saving semantic model to '{SemanticModelName}'.";
        public const string EnrichSemanticModelComplete = "Semantic model enrichment complete at '{ProjectPath}'.";
    }
}
