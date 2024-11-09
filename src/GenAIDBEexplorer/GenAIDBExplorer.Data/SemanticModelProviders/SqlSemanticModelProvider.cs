using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Models.SemanticModel;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public sealed class SqlSemanticModelProvider(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ILogger<SqlSemanticModelProvider> logger
) : ISemanticModelProvider, IDisposable
{
    private readonly IProject _project = project;
    private readonly IDatabaseConnectionProvider _connectionProvider = connectionProvider;
    private readonly ILogger _logger = logger;
    private SqlConnection? _connection;
    private bool _disposed = false;

    /// <summary>
    /// Builds the semantic model asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the built <see cref="SemanticModel"/>.</returns>
    public async Task<SemanticModel> BuildSemanticModelAsync()
    {
        _logger.LogInformation("Building semantic model for database {DatabaseName}", _project.Settings.Database.Name);

        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        var tablesDictionary = await GetTableListAsync().ConfigureAwait(false);
        foreach (var (_, table) in tablesDictionary)
        {
            _logger.LogInformation("Adding table {SchemaName}.{TableName} to the semantic model", table.SchemaName, table.TableName);

            var semanticModelTable = await CreateSemanticModelTableAsync(table);
            semanticModel.AddTable(semanticModelTable);
        }

        return semanticModel;
    }

    /// <summary>
    /// Connects to the database asynchronously if not already connected.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not open after attempting to connect.</exception>
    private async Task ConnectDatabaseAsync()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _logger.LogInformation("Connecting to database {DatabaseName} at {ConnectionString}", _project.Settings.Database.Name, _project.Settings.Database.ConnectionString);

            _connection = await _connectionProvider.ConnectAsync().ConfigureAwait(false);

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection is not open.");
            }
        }
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
        await ConnectDatabaseAsync().ConfigureAwait(false);

        var tables = new Dictionary<string, TableInfo>();

        try
        {
            var query = Statements.DescribeTables;
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
    /// Creates a Semantic Model Table for the specified table by querying the columns and keys.
    /// </summary>
    /// <param name="table">The table info for the table to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    private async Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table)
    {
        await ConnectDatabaseAsync().ConfigureAwait(false);

        var semanticModelTable = new SemanticModelTable(table.SchemaName, table.TableName);
        
        // Get the columns for the table
        var columns = await GetColumnsForTableAsync(table).ConfigureAwait(false);
        semanticModelTable.Columns.AddRange(columns);

        return semanticModelTable;
    }

    /// <summary>
    /// Retrieves the columns for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified table.</returns>
    private async Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table)
    {
        await ConnectDatabaseAsync().ConfigureAwait(false);

        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = Statements.DescribeColumns;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await ExecuteQueryAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var columnName = reader.GetString(2);
                var columnDesc = reader.IsDBNull(3) ? null : reader.GetString(3);
                var columnType = reader.GetString(4);
                var isPK = reader.GetBoolean(5);
                var isView = reader.GetBoolean(6);
                var column = new SemanticModelColumn(
                    name: columnName,
                    description: columnDesc,
                    type: columnType,
                    isPrimaryKey: isPK
                );
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
        using var cmd = _connection.CreateCommand();

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }

    ~SqlSemanticModelProvider()
    {
        Dispose(false);
    }

    // Represents a table in the database
    internal class TableInfo(string schemaName, string tableName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string TableName { get; set; } = tableName;
    }


    /// <summary>
    /// Contains SQL statements used by the <see cref="SqlSemanticModelProvider"/>.
    /// </summary>
    internal static class Statements
    {
        /// <summary>
        /// SQL query to describe tables, including schema name, table name, and table description.
        /// </summary>
        public const string DescribeTables = @"
SELECT 
    S.name AS SchemaName,
    O.name AS TableName,
    ep.value AS TableDesc
FROM 
    sys.tables O
JOIN 
    sys.schemas S ON O.schema_id = S.schema_id
LEFT JOIN 
    sys.extended_properties EP ON ep.major_id = O.object_id 
    AND ep.name = 'MS_DESCRIPTION' 
    AND ep.minor_id = 0
ORDER BY 
    S.name, 
    O.name;
";

        /// <summary>
        /// SQL query to describe columns for a specified table, including column name, data type, and column description.
        /// </summary>
        public const string DescribeColumns = @"
SELECT
    sch.name AS SchemaName,
    tab.name AS TableName,
    col.name AS ColumnName,
    ep.value AS ColumnDesc,
    base.name AS ColumnType,
    CAST(IIF(ic.column_id IS NULL, 0, 1) AS bit) IsPK,
    tab.IsView
FROM 
    (
        select object_id, schema_id, name, CAST(0 as bit) IsView from sys.tables
        UNION ALL
        select object_id, schema_id, name, CAST(1 as bit) IsView from sys.views
    ) tab
INNER JOIN
    sys.objects obj ON obj.object_id = tab.object_id
INNER JOIN
    sys.schemas sch ON tab.schema_id = sch.schema_id
INNER JOIN
    sys.columns col ON col.object_id = tab.object_id
INNER JOIN
    sys.types t ON col.user_type_id = t.user_type_id
INNER JOIN
    sys.types base ON t.system_type_id = base.user_type_id
LEFT OUTER JOIN 
    sys.indexes pk ON tab.object_id = pk.object_id AND pk.is_primary_key = 1
LEFT OUTER JOIN
    sys.index_columns ic ON ic.object_id = pk.object_id AND ic.index_id = pk.index_id AND ic.column_id = col.column_id 
LEFT OUTER JOIN
    sys.extended_properties ep ON ep.major_id = col.object_id AND ep.minor_id = col.column_id and ep.name = 'MS_DESCRIPTION'
WHERE
    sch.name != 'sys'
    AND sch.name = @SchemaName
    AND tab.name = @TableName
ORDER BY
    SchemaName, TableName, IsPK DESC, ColumnName
";

        public const string DescribeReferences = @"
SELECT
    obj.name AS KeyName,
    sch.name AS SchemaName,
    parentTab.name AS TableName,
    parentCol.name AS ColumnName,
    refTable.name AS ReferencedTableName,
    refCol.name AS ReferencedColumnName
FROM
    sys.foreign_key_columns fkc
INNER JOIN
    sys.objects obj ON obj.object_id = fkc.constraint_object_id
INNER JOIN
    sys.tables parentTab ON parentTab.object_id = fkc.parent_object_id
INNER JOIN
    sys.schemas sch ON parentTab.schema_id = sch.schema_id
INNER JOIN
    sys.columns parentCol ON parentCol.column_id = parent_column_id AND parentCol.object_id = parentTab.object_id
INNER JOIN
    sys.tables refTable ON refTable.object_id = fkc.referenced_object_id
INNER JOIN
    sys.columns refCol ON refCol.column_id = referenced_column_id AND refCol.object_id = refTable.object_id
";
    }
}