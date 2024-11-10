using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Models.SemanticModel;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public sealed class SchemaRepository(
    ISqlQueryExecutor sqlQueryExecutor,
    ILogger<SchemaRepository> logger
) : ISchemaRepository
{
    private readonly ISqlQueryExecutor _sqlQueryExecutor = sqlQueryExecutor;
    private readonly ILogger<SchemaRepository> _logger = logger;

    public async Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null)
    {
        var tables = new Dictionary<string, TableInfo>();
        var query = SqlStatements.DescribeTables;

        if (!string.IsNullOrEmpty(schema))
        {
            query += " WHERE S.name = @Schema";
        }

        var parameters = new Dictionary<string, object>
        {
            { "@Schema", schema ?? string.Empty }
        };

        using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            tables.Add($"{schemaName}.{tableName}", new TableInfo(schemaName, tableName));
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
    public async Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null)
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

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

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
    /// Retrieves a list of stored procedures from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter stored procedures by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and procedure name, and the value is the procedure name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    public async Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null)
    {
        var storedProcedures = new Dictionary<string, StoredProcedureInfo>();

        try
        {
            var query = SqlStatements.DescribeStoredProcedures;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " WHERE schema_name(obj.schema_id) = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var procedureName = reader.GetString(1);
                var procedureType = reader.GetString(2);
                var parametersList = reader.IsDBNull(3) ? null : reader.GetString(3);
                var definition = reader.GetString(4);
                storedProcedures.Add($"{schemaName}.{procedureName}", new StoredProcedureInfo(schemaName, procedureName, procedureType, parametersList, definition));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stored procedures from the database.");
            throw;
        }

        return storedProcedures;
    }

    /// <summary>
    /// Creates a Semantic Model Table for the specified table by querying the columns and keys.
    /// </summary>
    /// <param name="table">The table info for the table to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    public async Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table)
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
    public async Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view)
    {
        var semanticModelView = new SemanticModelView(view.SchemaName, view.ViewName);

        // Get the columns for the view
        var columns = await GetColumnsForViewAsync(view).ConfigureAwait(false);
        semanticModelView.Columns.AddRange(columns);

        return semanticModelView;
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
            storedProcedure.Definition,
            storedProcedure.Parameters
        );

        return Task.FromResult(semanticModelStoredProcedure);
    }

    /// <summary>
    /// Retrieves the columns for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified table.</returns>
    public async Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table)
    {
        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = SqlStatements.DescribeTableColumns;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // get the contents of column schemaName in the reader into a var 
                var columnName = reader.GetString(2);
                var columnType = reader.GetString(4);
                var column = new SemanticModelColumn(table.SchemaName, columnName, columnType)
                {
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsPrimaryKey = reader.GetBoolean(5),
                    MaxLength = reader.GetInt16(6),
                    Precision = reader.GetByte(7),
                    Scale = reader.GetByte(8),
                    IsNullable = reader.GetBoolean(9),
                    IsIdentity = reader.GetBoolean(10),
                    IsComputed = reader.GetBoolean(11),
                    IsXmlDocument = reader.GetBoolean(12)
                };
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
    /// Retrieves the columns for the specified view.
    /// </summary>
    /// <param name="view">The view info for the view to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified view.</returns>
    public async Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view)
    {
        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = SqlStatements.DescribeViewColumns;
            var parameters = new Dictionary<string, object> {
            { "@SchemaName", view.SchemaName },
            { "@ViewName", view.ViewName }
        };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var columnName = reader.GetString(2);
                var columnType = reader.GetString(4);
                var column = new SemanticModelColumn(view.SchemaName, columnName, columnType)
                {
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    MaxLength = reader.GetInt16(5),
                    Precision = reader.GetByte(6),
                    Scale = reader.GetByte(7),
                    IsNullable = reader.GetBoolean(8),
                    IsIdentity = reader.GetBoolean(9),
                    IsComputed = reader.GetBoolean(10),
                    IsXmlDocument = reader.GetBoolean(11)
                };
                semanticModelColumns.Add(column);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve columns for view {schemaName}.{viewName}", view.SchemaName, view.ViewName);
            throw;
        }

        return semanticModelColumns;
    }
}

// Represents a table in the database
public record TableInfo(string SchemaName, string TableName);

// Represents a view in the database
public record ViewInfo(string SchemaName, string ViewName);

// Represents a stored procedure in the database
public record StoredProcedureInfo(string SchemaName, string ProcedureName, string ProcedureType, string? Parameters, string Definition);
