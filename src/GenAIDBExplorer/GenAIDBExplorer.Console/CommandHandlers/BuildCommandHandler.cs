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
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to build.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class BuildCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<BuildCommandHandlerOptions>> logger
) : CommandHandler<BuildCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    /// <summary>
    /// Sets up the build command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The build command.</returns>
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
            description: "Flag to skip tables during the build process.",
            getDefaultValue: () => false
        );

        var skipViewsOption = new Option<bool>(
            aliases: ["--skipViews"],
            description: "Flag to skip views during the build process.",
            getDefaultValue: () => false
        );

        var skipStoredProceduresOption = new Option<bool>(
            aliases: ["--skipStoredProcedures"],
            description: "Flag to skip stored procedures during the build process.",
            getDefaultValue: () => false
        );

        var singleModelFileOption = new Option<bool>(
            aliases: ["--singleModelFile"],
            description: "Flag to produce the model as a single file.",
            getDefaultValue: () => false
        );

        var buildCommand = new Command("build", "Build a GenAI Database Explorer project.");
        buildCommand.AddOption(projectPathOption);
        buildCommand.AddOption(skipTablesOption);
        buildCommand.AddOption(skipViewsOption);
        buildCommand.AddOption(skipStoredProceduresOption);
        buildCommand.AddOption(singleModelFileOption);
        buildCommand.SetHandler(async (DirectoryInfo projectPath, bool skipTables, bool skipViews, bool skipStoredProcedures, bool singleModelFile) =>
        {
            var handler = host.Services.GetRequiredService<BuildCommandHandler>();
            var options = new BuildCommandHandlerOptions(projectPath, skipTables, skipViews, skipStoredProcedures, singleModelFile);
            await handler.HandleAsync(options);
        }, projectPathOption, skipTablesOption, skipViewsOption, skipStoredProceduresOption, singleModelFileOption);

        return buildCommand;
    }

    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(BuildCommandHandlerOptions commandOptions)
    {
        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation(LogMessages.BuildingProject, projectPath.FullName);

        _project.LoadConfiguration(projectPath.FullName);

        // Build the Semantic Model
        var semanticModel = await _semanticModelProvider.BuildSemanticModelAsync().ConfigureAwait(false);

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

        // Save the Semantic Model into the project directory into a subdirectory with the name of the semanticModel.Name
        var semanticModelDirectory = new DirectoryInfo(Path.Combine(projectPath.FullName, semanticModel.Name));
        if (!semanticModelDirectory.Exists)
        {
            semanticModelDirectory.Create();
        }

        _logger.LogInformation(LogMessages.SavingSemanticModel, semanticModelDirectory);
        semanticModel.SaveModel(semanticModelDirectory, !commandOptions.SingleModelFile);

        _logger.LogInformation(LogMessages.ProjectBuildComplete, projectPath.FullName);
    }

    /// <summary>
    /// Contains log messages used in the <see cref="BuildCommandHandler"/> class.
    /// </summary>
    public static class LogMessages
    {
        public const string BuildingProject = "Building project at '{ProjectPath}'.";
        public const string SavingSemanticModel = "Saving semantic model to '{SemanticModelName}'.";
        public const string ProjectBuildComplete = "Project build complete at '{ProjectPath}'.";
    }
}