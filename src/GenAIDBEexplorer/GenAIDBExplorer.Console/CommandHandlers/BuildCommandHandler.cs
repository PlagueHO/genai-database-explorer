using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Data.DatabaseProviders;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to build.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class BuildCommandHandler(IProject project, IDatabaseConnectionProvider connectionProvider, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(project, connectionProvider, serviceProvider, logger)
{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation(LogMessages.BuildingProject, projectPath.FullName);

        _project.LoadConfiguration(projectPath.FullName);

        // Get a database connection from the SqlConnectionProvider
        using var connection = _connectionProvider.ConnectAsync().GetAwaiter().GetResult();

        _logger.LogInformation(LogMessages.DatabaseConnectionState, connection.State);

        // Assemble the Semantic Model

        // Close the connection
        connection.Close();
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