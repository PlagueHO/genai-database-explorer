using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Models.SemanticModel;
using System.Collections.Concurrent;
using GenAIDBExplorer.Data.ConnectionManager;
using GenAIDBExplorer.Data.DatabaseProviders;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public sealed class SqlSemanticModelProvider(
    IProject project,
    ISchemaRepository schemaRepository,
    ISqlQueryExecutor sqlQueryExecutor,
    ILogger<SqlSemanticModelProvider> logger
) : ISemanticModelProvider
{
    private readonly IProject _project = project;
    private readonly ISqlQueryExecutor _sqlQueryExecutor = sqlQueryExecutor;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Builds the semantic model asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the built <see cref="SemanticModel"/>.</returns>
    public async Task<SemanticModel> BuildSemanticModelAsync()
    {
        _logger.LogInformation("Building semantic model for database {DatabaseName}", _project.Settings.Database.Name);

        // Create the new SemanticModel instance to build
        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        // Configure the parallel options for the operation
        var options = new ParallelOptions {
            MaxDegreeOfParallelism = _project.Settings.Database.MaxDegreeOfParallelism
        };

        // Get the tables from the database
        var tablesDictionary = await schemaRepository.GetTablesAsync().ConfigureAwait(false);
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
        var viewsDictionary = await schemaRepository.GetViewsAsync().ConfigureAwait(false);
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

        return semanticModel;
    }
}