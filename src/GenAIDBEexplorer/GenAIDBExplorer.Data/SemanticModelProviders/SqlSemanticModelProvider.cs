using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Models.SemanticModel;
using System.Collections.Concurrent;
using GenAIDBExplorer.Data.ConnectionManager;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public sealed class SqlSemanticModelProvider(
    IProject project,
    IDatabaseConnectionManager connectionManager,
    ILogger<SqlSemanticModelProvider> logger
) : ISemanticModelProvider
{
    private readonly IProject _project = project;
    private readonly IDatabaseConnectionManager _connectionManager = connectionManager;
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
        var tablesDictionary = await GetTableListAsync().ConfigureAwait(false);
        var semanticModelTables = new ConcurrentBag<SemanticModelTable>();

        // Construct the semantic model tables
        await Parallel.ForEachAsync(tablesDictionary.Values, options, async (table, cancellationToken) =>
        {
            _logger.LogInformation("Adding table {SchemaName}.{TableName} to the semantic model", table.SchemaName, table.TableName);

            var semanticModelTable = await CreateSemanticModelTableAsync(table).ConfigureAwait(false);
            semanticModelTables.Add(semanticModelTable);
        });

        // Add the tables to the semantic model
        semanticModel.Tables.AddRange(semanticModelTables);

        // Get the views from the database
        var viewsDictionary = await GetViewListAsync().ConfigureAwait(false);
        var semanticModelViews = new ConcurrentBag<SemanticModelView>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(viewsDictionary.Values, options, async (view, cancellationToken) =>
        {
            _logger.LogInformation("Adding view {SchemaName}.{ViewName} to the semantic model", view.SchemaName, view.ViewName);

            var semanticModelView = await CreateSemanticModelViewAsync(view).ConfigureAwait(false);
            semanticModelViews.Add(semanticModelView);
        });

        // Add the view to the semantic model
        semanticModel.Views.AddRange(semanticModelViews);

        return semanticModel;

    }

    /// <summary>
    /// Retrieves a list of tables from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter tables by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and table name, and the value is the table name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    private async Task<Dictionary<string, TableInfo>> GetTableListAsync(string? schema = null)
    {
        var tables = new Dictionary<string, TableInfo>();

        try
        {
            var query = SqlStatements.DescribeTables;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " WHERE S.name = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await ExecuteQueryAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var tableName = reader.GetString(1);
                tables.Add($"{schemaName}.{tableName}", new TableInfo(schemaName, tableName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tables from the database.");
            throw;
        }

        return tables;
    }

    /// <summary>
    /// Retrieves a list of views from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter view by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and table name, and the value is the table name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    private async Task<Dictionary<string, ViewInfo>> GetViewListAsync(string? schema = null)
    {
        var views = new Dictionary<string, ViewInfo>();

        try
        {
            var query = SqlStatements.DescribeViews;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " WHERE S.name = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await ExecuteQueryAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var viewName = reader.GetString(1);
                views.Add($"{schemaName}.{viewName}", new ViewInfo(schemaName, viewName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve views from the database.");
            throw;
        }

        return views;
    }

    /// <summary>
    /// Creates a Semantic Model Table for the specified table by querying the columns and keys.
    /// </summary>
    /// <param name="table">The table info for the table to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    private async Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table)
    {
        var semanticModelTable = new SemanticModelTable(table.SchemaName, table.TableName);
        
        // Get the columns for the table
        var columns = await GetColumnsForTableAsync(table).ConfigureAwait(false);
        semanticModelTable.Columns.AddRange(columns);

        return semanticModelTable;
    }

    /// <summary>
    /// Creates a Semantic Model View for the specified view by querying the columns and keys.
    /// </summary>
    /// <param name="view">The view info for the view to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    private async Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view)
    {
        var semanticModelView = new SemanticModelView(view.SchemaName, view.ViewName);

        // Get the columns for the view
        // var columns = await GetColumnsForViewAsync(view).ConfigureAwait(false);
        // semanticModelView.Columns.AddRange(columns);

        return semanticModelView;
    }

    /// <summary>
    /// Retrieves the columns for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified table.</returns>
    private async Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table)
    {
        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = SqlStatements.DescribeTableColumns;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await ExecuteQueryAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // get the contents of column schemaName in the reader into a var 
                var columnName = reader.GetString(2);
                var columnType = reader.GetString(4);
                var column = new SemanticModelColumn(table.SchemaName, columnName, columnType);
                column.Description = reader.IsDBNull(3) ? null : reader.GetString(3);
                column.IsPrimaryKey = reader.GetBoolean(5);
                column.MaxLength = reader.GetInt16(6);
                column.Precision = reader.GetByte(7);
                column.Scale = reader.GetByte(8);
                column.IsNullable = reader.GetBoolean(9);
                column.IsIdentity = reader.GetBoolean(10);
                column.IsComputed = reader.GetBoolean(11);
                column.IsXmlDocument = reader.GetBoolean(12);
                semanticModelColumns.Add(column);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve columns for table {schemaName}.{tableName}", table.SchemaName, table.TableName);
            throw;

        }
        
        return semanticModelColumns;
    }

    /// <summary>
    /// Executes the provided SQL query asynchronously and returns a <see cref="SqlDataReader"/> to read the results.
    /// </summary>
    /// <param name="statement">The SQL query to be executed.</param>
    /// <param name="parameters">The SQL parameters to be added to the query.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="SqlDataReader"/> to read the query results.</returns>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    private async Task<SqlDataReader> ExecuteQueryAsync(string statement, Dictionary<string, object>? parameters = null)
    {
        var connection = await _connectionManager.GetOpenConnectionAsync().ConfigureAwait(false);

        using var cmd = connection.CreateCommand();

        // Log the SQL query being executed
        _logger.LogDebug("Executing SQL query: {SqlQuery}", statement);

#pragma warning disable CA2100 // Queries passed in from static resource
        cmd.CommandText = statement;
#pragma warning restore CA2100

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
        }

        return await cmd.ExecuteReaderAsync().ConfigureAwait(false);
    }

    // Represents a table in the database
    internal class TableInfo(string schemaName, string tableName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string TableName { get; set; } = tableName;
    }

    // Represents a view in the database
    internal class ViewInfo(string schemaName, string viewName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string ViewName { get; set; } = viewName;
    }
}