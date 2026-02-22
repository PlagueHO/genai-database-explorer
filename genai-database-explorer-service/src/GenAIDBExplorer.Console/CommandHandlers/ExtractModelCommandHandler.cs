using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Console.Services;
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
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class ExtractModelCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<ExtractModelCommandHandlerOptions>> logger
) : CommandHandler<ExtractModelCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(ExtractModelCommandHandler).Assembly);

    /// <summary>
    /// Sets up the extract model command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The extract model command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project.",
            Required = true
        };

        var skipTablesOption = new Option<bool>("--skip-tables")
        {
            Description = "Flag to skip tables during the semantic model extraction process."
        };

        var skipViewsOption = new Option<bool>("--skip-views")
        {
            Description = "Flag to skip views during the semantic model extraction process."
        };

        var skipStoredProceduresOption = new Option<bool>("--skip-stored-procedures")
        {
            Description = "Flag to skip stored procedures during the extract model process."
        };

        var extractModelCommand = new Command("extract-model", "Extract a semantic model from a SQL database for a GenAI Database Explorer project.");
        extractModelCommand.Options.Add(projectPathOption);
        extractModelCommand.Options.Add(skipTablesOption);
        extractModelCommand.Options.Add(skipViewsOption);
        extractModelCommand.Options.Add(skipStoredProceduresOption);
        extractModelCommand.SetAction(async (parseResult) =>
        {
            var projectPath = parseResult.GetValue(projectPathOption)!;
            var skipTables = parseResult.GetValue(skipTablesOption);
            var skipViews = parseResult.GetValue(skipViewsOption);
            var skipStoredProcedures = parseResult.GetValue(skipStoredProceduresOption);

            var handler = host.Services.GetRequiredService<ExtractModelCommandHandler>();
            var options = new ExtractModelCommandHandlerOptions(projectPath, skipTables, skipViews, skipStoredProcedures);

            try
            {
                await handler.HandleAsync(options);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                // Authorization failure - log error and exit with non-zero code
                System.Console.Error.WriteLine($"AuthorizationFailure: {ex.Message}");
                System.Console.Error.WriteLine("This request is not authorized to perform this operation.");
                System.Console.Error.WriteLine("This may indicate missing 'Storage Blob Data Contributor' role assignment for the configured Azure storage.");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                // Other failures - log and exit
                System.Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return extractModelCommand;
    }

    /// <summary>
    /// Handles the extract model command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(ExtractModelCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("ExtractingSemanticModel"), projectPath.FullName);

        _project.LoadProjectConfiguration(projectPath);

        // Extract the Semantic Model
        var semanticModel = await _semanticModelProvider.ExtractSemanticModelAsync().ConfigureAwait(false);

        // Save the Semantic Model using the configured persistence strategy
        _logger.LogInformation("{Message}", _resourceManagerLogMessages.GetString("SavingSemanticModel"));
        await _semanticModelProvider.SaveSemanticModelAsync(semanticModel).ConfigureAwait(false);

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("ExtractSemanticModelComplete"), projectPath.FullName);
    }
}