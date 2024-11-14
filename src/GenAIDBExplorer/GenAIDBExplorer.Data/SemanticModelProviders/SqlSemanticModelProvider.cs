using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Models.SemanticModel;
using System.Collections.Concurrent;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public sealed class SqlSemanticModelProvider(
    IProject project,
    ISchemaRepository schemaRepository,
    ILogger<SqlSemanticModelProvider> logger
) : ISemanticModelProvider
{
    private readonly IProject _project = project;
    private readonly ILogger _logger = logger;

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
        _logger.LogInformation("Loading semantic model from {modelPath}", modelPath);

        var semanticModel = await CreateSemanticModel().LoadModelAsync(modelPath);

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> ExtractSemanticModelAsync()
    {
        _logger.LogInformation("Extracting semantic model for database {DatabaseName}", _project.Settings.Database.Name);

        // Create the new SemanticModel instance to build
        var semanticModel = CreateSemanticModel();

        // Configure the parallel options for the operation
        var options = new ParallelOptions {
            MaxDegreeOfParallelism = _project.Settings.Database.MaxDegreeOfParallelism
        };

        // Get the tables from the database
        var tablesDictionary = await schemaRepository.GetTablesAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelTables = new ConcurrentBag<SemanticModelTable>();

        // Construct the semantic model tables
        await Parallel.ForEachAsync(tablesDictionary.Values, options, async (table, cancellationToken) =>
        {
            _logger.LogInformation("Adding table {SchemaName}.{TableName} to the semantic model", table.SchemaName, table.TableName);

            var semanticModelTable = await schemaRepository.CreateSemanticModelTableAsync(table).ConfigureAwait(false);
            semanticModelTables.Add(semanticModelTable);
        });

        // Add the tables to the semantic model
        semanticModel.Tables.AddRange(semanticModelTables);

        // Get the views from the database
        var viewsDictionary = await schemaRepository.GetViewsAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelViews = new ConcurrentBag<SemanticModelView>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(viewsDictionary.Values, options, async (view, cancellationToken) =>
        {
            _logger.LogInformation("Adding view {SchemaName}.{ViewName} to the semantic model", view.SchemaName, view.ViewName);

            var semanticModelView = await schemaRepository.CreateSemanticModelViewAsync(view).ConfigureAwait(false);
            semanticModelViews.Add(semanticModelView);
        });

        // Add the view to the semantic model
        semanticModel.Views.AddRange(semanticModelViews);

        // Get the stored procedures from the database
        var storedProceduresDictionary = await schemaRepository.GetStoredProceduresAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelStoredProcedures = new ConcurrentBag<SemanticModelStoredProcedure>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(storedProceduresDictionary.Values, options, async (storedProcedure, cancellationToken) =>
        {
            _logger.LogInformation("Adding stored procedure {SchemaName}.{StoredProcedure} to the semantic model", storedProcedure.SchemaName, storedProcedure.ProcedureName);

            var semanticModeStoredProcedure = await schemaRepository.CreateSemanticModelStoredProcedureAsync(storedProcedure).ConfigureAwait(false);
            semanticModelStoredProcedures.Add(semanticModeStoredProcedure);
        });

        // Add the stored procedures to the semantic model
        semanticModel.StoredProcedures.AddRange(semanticModelStoredProcedures);

        // return the semantic model Task
        return semanticModel;
    }

    /// <summary>
    /// Creates a Semantic Model Stored Procedure for the specified stored procedure info.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure info to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelStoredProcedure"/>.</returns>
    public Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure)
    {
        var semanticModelStoredProcedure = new SemanticModelStoredProcedure(
            storedProcedure.SchemaName,
            storedProcedure.ProcedureName,
            storedProcedure.Parameters,
            storedProcedure.Definition
        );

        return Task.FromResult(semanticModelStoredProcedure);
    }

}