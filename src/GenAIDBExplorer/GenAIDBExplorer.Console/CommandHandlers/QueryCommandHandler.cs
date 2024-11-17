using GenAIDBExplorer.AI.SemanticProviders;
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
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to query.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class QueryCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<QueryCommandHandlerOptions>> logger
) : CommandHandler<QueryCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(QueryCommandHandler).Assembly);

    /// <summary>
    /// Sets up the query command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The query command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>(
            aliases: [ "--project", "-p" ],
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        var queryCommand = new Command("query", "Query a GenAI Database Explorer project.");
        queryCommand.AddOption(projectPathOption);
        queryCommand.SetHandler(async (DirectoryInfo projectPath) =>
        {
            var handler = host.Services.GetRequiredService<QueryCommandHandler>();
            var options = new QueryCommandHandlerOptions(projectPath);
            await handler.HandleAsync(options);
        }, projectPathOption);

        return queryCommand;
    }

    /// <summary>
    /// Handles the query command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(QueryCommandHandlerOptions commandOptions)
    {
        var projectPath = commandOptions.ProjectPath;

        _project.LoadProjectConfiguration(projectPath);

        // Load the Semantic Model
        _logger.LogInformation(_resourceManagerLogMessages.GetString("LoadingSemanticModel"), projectPath.FullName);
        var semanticModelDirectory = new DirectoryInfo(Path.Combine(projectPath.FullName, _project.Settings.Database.Name));
        var semanticModel = await _semanticModelProvider.LoadSemanticModelAsync(semanticModelDirectory);
        _logger.LogInformation(_resourceManagerLogMessages.GetString("LoadedSemanticModel"), projectPath.FullName);

        _logger.LogInformation(_resourceManagerLogMessages.GetString("QueryingProject"), projectPath.FullName);

        await Task.CompletedTask;
    }
}
