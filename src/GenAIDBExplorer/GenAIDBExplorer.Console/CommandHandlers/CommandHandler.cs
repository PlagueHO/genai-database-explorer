using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.Logging;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Abstract base class for command handlers.
/// </summary>
/// <remarks>
/// This class provides common utility functionality for handling console commands.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandHandler"/> class.
/// </remarks>
public abstract class CommandHandler<TOptions>(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<TOptions>> logger
) : ICommandHandler<TOptions> where TOptions : ICommandHandlerOptions
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(EnrichModelCommandHandler).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Console.Resources.ErrorMessages", typeof(EnrichModelCommandHandler).Assembly);

    /// <summary>
    /// Project instance to handle.
    /// </summary>
    protected readonly IProject _project = project ?? throw new ArgumentNullException(nameof(project));

    /// <summary>
    /// Database connection provider instance for connecting to a SQL database.
    /// </summary>
    protected readonly IDatabaseConnectionProvider _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));

    /// <summary>
    /// Semantic model provider instance for building a semantic model of the database.
    /// </summary>
    protected readonly ISemanticModelProvider _semanticModelProvider = semanticModelProvider ?? throw new ArgumentNullException(nameof(semanticModelProvider));

    /// <summary>
    /// Output service instance for outputting information, warnings, and errors.
    /// </summary>
    protected readonly IOutputService _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));

    /// <summary>
    /// Service provider instance for resolving dependencies.
    /// </summary>
    protected readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <summary>
    /// Logger instance for logging information, warnings, and errors.
    /// </summary>
    protected readonly ILogger<ICommandHandler<TOptions>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="commandOptions">The command options that were provided to the command.</param>
    public abstract Task HandleAsync(TOptions commandOptions);

    /// <summary>
    /// Asserts that the command options and its properties are valid.
    /// </summary>
    /// <param name="commandOptions">The command options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the command options or its properties are null.</exception>
    protected void AssertCommandOptionsValid(TOptions commandOptions)
    {
        if (commandOptions == null)
        {
            throw new ArgumentNullException(nameof(commandOptions), "Command options cannot be null.");
        }

        if (commandOptions.ProjectPath == null)
        {
            throw new ArgumentNullException(nameof(commandOptions), "Project path cannot be null.");
        }
    }

    /// <summary>
    /// Validates the specified project path.
    /// </summary>
    /// <param name="projectPath">The project path to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the project path is null.</exception>
    protected static void ValidateProjectPath(DirectoryInfo projectPath)
    {
        if (projectPath == null)
        {
            throw new ArgumentNullException(nameof(projectPath), "Project path cannot be null.");
        }
    }

    /// <summary>
    /// Outputs an informational message to the console.
    /// </summary>
    /// <param name="message">The message to output.</param>
    protected void OutputInformation(string message)
    {
        _outputService.WriteLine(message);
    }

    /// <summary>
    /// Outputs a warning message to the console in yellow text.
    /// </summary>
    /// <param name="message">The message to output.</param>
    protected void OutputWarning(string message)
    {
        _outputService.WriteWarning(message);
    }

    /// <summary>
    /// Output a stop error to the console as red text.
    /// </summary>
    /// <param name="message">The message to output.</param>
    protected void OutputStopError(string message)
    {
        _outputService.WriteError(message);
    }

    /// <summary>
    /// Loads the semantic model from the specified project path.
    /// </summary>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The loaded semantic model.</returns>
    protected async Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo projectPath)
    {
        _project.LoadProjectConfiguration(projectPath);

        // Load the Semantic Model using the configured persistence strategy
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("LoadingSemanticModel"), projectPath.FullName);
        var semanticModel = await _semanticModelProvider.LoadSemanticModelAsync();
        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("LoadedSemanticModel"), projectPath.FullName);

        return semanticModel;
    }

    /// <summary>
    /// Gets the semantic model directory for the specified project path using the configured persistence strategy.
    /// </summary>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The semantic model directory path.</returns>
    /// <exception cref="NotSupportedException">Thrown when the persistence strategy is not LocalDisk.</exception>
    /// <exception cref="InvalidOperationException">Thrown when LocalDisk directory is not configured.</exception>
    protected DirectoryInfo GetSemanticModelDirectory(DirectoryInfo projectPath)
    {
        var persistenceStrategy = _project.Settings.SemanticModel.PersistenceStrategy;
        
        return persistenceStrategy switch
        {
            "LocalDisk" => GetLocalDiskSemanticModelDirectory(projectPath),
            "AzureBlob" => throw new NotSupportedException($"Persistence strategy '{persistenceStrategy}' is not yet supported for saving semantic models."),
            "Cosmos" => throw new NotSupportedException($"Persistence strategy '{persistenceStrategy}' is not yet supported for saving semantic models."),
            _ => throw new ArgumentException($"Unknown persistence strategy '{persistenceStrategy}' specified in project settings.")
        };
    }

    /// <summary>
    /// Gets the local disk semantic model directory path.
    /// </summary>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The local disk semantic model directory.</returns>
    private DirectoryInfo GetLocalDiskSemanticModelDirectory(DirectoryInfo projectPath)
    {
        var localDiskConfig = _project.Settings.SemanticModelRepository?.LocalDisk;
        if (localDiskConfig?.Directory == null || string.IsNullOrWhiteSpace(localDiskConfig.Directory))
        {
            throw new InvalidOperationException("LocalDisk persistence strategy is configured but no directory is specified in SemanticModelRepository.LocalDisk.Directory.");
        }

        return new DirectoryInfo(Path.Combine(projectPath.FullName, localDiskConfig.Directory));
    }

    protected Task ShowTableDetailsAsync(SemanticModel semanticModel, string schemaName, string tableName)
    {
        var table = semanticModel.FindTable(schemaName, tableName);
        if (table == null)
        {
            var errorMessage = _resourceManagerErrorMessages.GetString("TableNotFound") ?? "Table not found";
            _logger.LogError("{ErrorMessage} [{SchemaName}].[{TableName}]", errorMessage, schemaName, tableName);
        }
        else
        {
            OutputInformation(table.ToString());
        }
        return Task.CompletedTask;
    }

    protected Task ShowViewDetailsAsync(SemanticModel semanticModel, string schemaName, string viewName)
    {
        var view = semanticModel.FindView(schemaName, viewName);
        if (view == null)
        {
            var errorMessage = _resourceManagerErrorMessages.GetString("ViewNotFound") ?? "View not found";
            _logger.LogError("{ErrorMessage} [{SchemaName}].[{ViewName}]", errorMessage, schemaName, viewName);
        }
        else
        {
            OutputInformation(view.ToString());
        }
        return Task.CompletedTask;
    }

    protected Task ShowStoredProcedureDetailsAsync(SemanticModel semanticModel, string schemaName, string storedProcedureName)
    {
        var storedProcedure = semanticModel.FindStoredProcedure(schemaName, storedProcedureName);
        if (storedProcedure == null)
        {
            var errorMessage = _resourceManagerErrorMessages.GetString("StoredProcedureNotFound") ?? "Stored procedure not found";
            _logger.LogError("{ErrorMessage} [{SchemaName}].[{StoredProcedureName}]", errorMessage, schemaName, storedProcedureName);
        }
        else
        {
            OutputInformation(storedProcedure.ToString());
        }
        return Task.CompletedTask;
    }
}