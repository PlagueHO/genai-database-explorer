using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.AI.SemanticProviders;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for initializing a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitProjectCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to initialize.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class InitProjectCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<InitProjectCommandHandlerOptions>> logger
) : CommandHandler<InitProjectCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(InitProjectCommandHandler).Assembly);

    /// <summary>
    /// Sets up the init-project command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The init-project command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>(
            aliases: ["--project", "-p"],
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        var initCommand = new Command("init-project", "Initialize a GenAI Database Explorer project.");
        initCommand.AddOption(projectPathOption);
        initCommand.SetHandler(async (DirectoryInfo projectPath) =>
        {
            var handler = host.Services.GetRequiredService<InitProjectCommandHandler>();
            var options = new InitProjectCommandHandlerOptions(projectPath);
            await handler.HandleAsync(options);
        }, projectPathOption);

        return initCommand;
    }

    /// <summary>
    /// Handles the initialization command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(InitProjectCommandHandlerOptions commandOptions)
    {
        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation(_resourceManagerLogMessages.GetString("InitializingProject"), projectPath.FullName);

        ValidateProjectPath(projectPath);

        // Initialize the project directory, but catch the exception if the directory is not empty
        try
        {
            _project.InitializeProjectDirectory(projectPath);
        }
        catch (Exception ex)
        {
            OutputStopError(ex.Message);
            return;
        }

        _logger.LogInformation(_resourceManagerLogMessages.GetString("InitializeProjectComplete"), projectPath.FullName);
        await Task.CompletedTask;
    }
}
