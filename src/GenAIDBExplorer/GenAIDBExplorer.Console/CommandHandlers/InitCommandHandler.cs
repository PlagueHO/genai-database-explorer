using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.AI.SemanticProviders;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for initializing a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to initialize.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="semanticDescriptionProvider">The semantic description provider instance for generating semantic descriptions.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class InitCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticDescriptionProvider semanticDescriptionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<InitCommandHandlerOptions>> logger
) : CommandHandler<InitCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, semanticDescriptionProvider, serviceProvider, logger)
{
    /// <summary>
    /// Handles the initialization command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(InitCommandHandlerOptions commandOptions)
    {
        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation(LogMessages.InitializingProject, projectPath.FullName);

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

        _logger.LogInformation(LogMessages.ProjectInitialized, projectPath.FullName);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Contains log messages used in the <see cref="InitCommandHandler"/> class.
    /// </summary>
    public static class LogMessages
    {
        public const string InitializingProject = "Initializing project at '{ProjectPath}'";
        public const string ProjectFolderNotEmpty = "The project folder is not empty. Please specify an empty folder.";
        public const string ProjectInitialized = "Project initialized successfully in '{ProjectPath}'.";
    }
}
