﻿using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Data.DatabaseProviders;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to query.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class QueryCommandHandler(IProject project, IDatabaseConnectionProvider connectionProvider, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(project, connectionProvider, serviceProvider, logger)
{
    /// <summary>
    /// Handles the query command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to query.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation(LogMessages.QueryingProject, projectPath.FullName);

        // Your query logic here
    }

    /// <summary>
    /// Contains log messages used in the <see cref="QueryCommandHandler"/> class.
    /// </summary>
    public static class LogMessages
    {
        public const string QueryingProject = "Querying project at '{ProjectPath}'.";
    }
}