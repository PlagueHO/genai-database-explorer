using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Resources;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public sealed class SemanticModelProvider(
    IProject project,
    ISchemaRepository schemaRepository,
    ILogger<SemanticModelProvider> logger
) : ISemanticModelProvider
{
    private readonly IProject _project = project;
    private readonly ISchemaRepository _schemaRepository = schemaRepository;
    private readonly ILogger _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SemanticModelProvider).Assembly);

    /// <inheritdoc/>
    public SemanticModel CreateSemanticModel()
    {
        // Create the new SemanticModel instance to build
        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath)
    {
        _logger.LogInformation("{Message} '{ModelPath}'", _resourceManagerLogMessages.GetString("LoadingSemanticModel"), modelPath);

        var semanticModel = await CreateSemanticModel().LoadModelAsync(modelPath);

        _logger.LogInformation("{Message} '{SemanticModelName}'", _resourceManagerLogMessages.GetString("LoadedSemanticModelForDatabase"), semanticModel.Name);

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> ExtractSemanticModelAsync()
    {
        _logger.LogInformation("{Message} '{DatabaseName}'", _resourceManagerLogMessages.GetString("ExtractingModelForDatabase"), _project.Settings.Database.Name);

        // Create the new SemanticModel instance to build
        var semanticModel = CreateSemanticModel();

        // Configure the parallel parallelOptions for the operation
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _project.Settings.Database.MaxDegreeOfParallelism
        };

        await ExtractSemanticModelTablesAsync(semanticModel, parallelOptions);

        await ExtractSemanticModelViewsAsync(semanticModel, parallelOptions);

        await ExtractSemanticModelStoredProceduresAsync(semanticModel, parallelOptions);

        // return the semantic model Task
        return semanticModel;
    }

    /// <summary>
    /// Extracts the tables from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the tables will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelTablesAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the tables from the database
        var tablesDictionary = await _schemaRepository.GetTablesAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelTables = new ConcurrentBag<SemanticModelTable>();

        // Construct the semantic model tables
        await Parallel.ForEachAsync(tablesDictionary.Values, parallelOptions, async (table, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("AddingTableToSemanticModel"), table.SchemaName, table.TableName);

            var semanticModelTable = await _schemaRepository.CreateSemanticModelTableAsync(table).ConfigureAwait(false);
            semanticModelTables.Add(semanticModelTable);
        });

        // Add the tables to the semantic model
        semanticModel.Tables.AddRange(semanticModelTables);
    }

    /// <summary>
    /// Extracts the views from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the views will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelViewsAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the views from the database
        var viewsDictionary = await _schemaRepository.GetViewsAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelViews = new ConcurrentBag<SemanticModelView>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(viewsDictionary.Values, parallelOptions, async (view, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("AddingViewToSemanticModel"), view.SchemaName, view.ViewName);

            var semanticModelView = await _schemaRepository.CreateSemanticModelViewAsync(view).ConfigureAwait(false);
            semanticModelViews.Add(semanticModelView);
        });

        // Add the view to the semantic model
        semanticModel.Views.AddRange(semanticModelViews);
    }

    /// <summary>
    /// Extracts the stored procedures from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the stored procedures will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelStoredProceduresAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the stored procedures from the database
        var storedProceduresDictionary = await _schemaRepository.GetStoredProceduresAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelStoredProcedures = new ConcurrentBag<SemanticModelStoredProcedure>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(storedProceduresDictionary.Values, parallelOptions, async (storedProcedure, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("AddingStoredProcedureToSemanticModel"), storedProcedure.SchemaName, storedProcedure.ProcedureName);

            var semanticModeStoredProcedure = await _schemaRepository.CreateSemanticModelStoredProcedureAsync(storedProcedure).ConfigureAwait(false);
            semanticModelStoredProcedures.Add(semanticModeStoredProcedure);
        });

        // Add the stored procedures to the semantic model
        semanticModel.StoredProcedures.AddRange(semanticModelStoredProcedures);
    }
}