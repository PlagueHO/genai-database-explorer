using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
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

        var enrichModelCommand = new Command("enrich-model", "Enrich an existing semantic model with descriptions in a GenAI Database Explorer project.");
        enrichModelCommand.AddOption(projectPathOption);
        enrichModelCommand.AddOption(skipTablesOption);
        enrichModelCommand.AddOption(skipViewsOption);
        enrichModelCommand.AddOption(skipStoredProceduresOption);
        enrichModelCommand.AddOption(singleModelFileOption);
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

        if (!commandOptions.SkipTables)
        {
            // Generate the Semantic Description for all tables
            await _semanticDescriptionProvider.UpdateTableSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }

        if (!commandOptions.SkipViews)
        {
            // Generate the Semantic Description for all views
            await _semanticDescriptionProvider.UpdateViewSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }

        if (!commandOptions.SkipStoredProcedures)
        {
            // Generate the Semantic Description for all stored procedures
            await _semanticDescriptionProvider.UpdateStoredProcedureSemanticDescriptionAsync(semanticModel).ConfigureAwait(false);
        }

        // Save the semantic model
        _logger.LogInformation(_resourceManagerLogMessages.GetString("SavingSemanticModel"), projectPath.FullName);
        var semanticModelDirectory = GetSemanticModelDirectory(projectPath);
        await semanticModel.SaveModelAsync(semanticModelDirectory, !commandOptions.SingleModelFile);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("SavedSemanticModel"), projectPath.FullName);

        _logger.LogInformation(_resourceManagerLogMessages.GetString("EnrichSemanticModelComplete"), projectPath.FullName);
    }
}
