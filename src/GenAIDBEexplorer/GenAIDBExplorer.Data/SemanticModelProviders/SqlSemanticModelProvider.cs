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

    public async Task<SemanticModel> BuildSemanticModelAsync()
    {
        _logger.LogInformation("Building semantic model for database {DatabaseName}", _project.Settings.Database.Name);

        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        var tablesDictionary = await GetTableListAsync().ConfigureAwait(false);

        foreach (var (tableKey, tableName) in tablesDictionary)
        {
            _logger.LogInformation("Adding table {TableName} to the semantic model", tableName);

            var table = new SemanticModelTable(
                name: tableName,
                description: tableKey
            );
            semanticModel.AddTable(table);
        }

        return semanticModel;
    }

    public async Task<Dictionary<string, string>> GetTableListAsync()
    {
        await ConnectDatabaseAsync().ConfigureAwait(false);

        var tables = new Dictionary<string, string>();

        try
        {
            using var reader = await ExecuteQueryAsync(Statements.DescribeTables).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schema = reader.GetString(0);
                var tableName = reader.GetString(1);
                tables.Add($"{schema}.{tableName}", tableName);
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
    /// Executes the provided SQL query asynchronously and returns a <see cref="SqlDataReader"/> to read the results.
    /// </summary>
    /// <param name="statement">The SQL query to be executed.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="SqlDataReader"/> to read the query results.</returns>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    private async Task<SqlDataReader> ExecuteQueryAsync(string statement)
    {
        using var cmd = _connection.CreateCommand();

        // Log the SQL query being executed
        _logger.LogDebug("Executing SQL query: {SqlQuery}", statement);

#pragma warning disable CA2100 // Queries passed in from static resource
        cmd.CommandText = statement;
#pragma warning restore CA2100

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
    }
}