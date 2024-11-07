using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Data.SemanticModelProviders;

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
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class BuildCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler> logger
) : CommandHandler(project, connectionProvider, semanticModelProvider, serviceProvider, logger)
{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation(LogMessages.BuildingProject, projectPath.FullName);

        _project.LoadConfiguration(projectPath.FullName);

        // Assemble the Semantic Model
        // Get a list of all the tables in the database
        var tables = _semanticModelProvider.GetTablesAsync().Result;


    }

    /// <summary>
    /// Contains log messages used in the <see cref="BuildCommandHandler"/> class.
    /// </summary>
    public static class LogMessages
    {
        public const string BuildingProject = "Building project at '{ProjectPath}'.";
        public const string DatabaseConnectionState = "Database connection state: {State}";
    }
}