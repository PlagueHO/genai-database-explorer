using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.ConnectionManager;
using GenAIDBExplorer.Data.SemanticModelProviders;
using System.Resources;

namespace GenAIDBExplorer.Data.DatabaseProviders;

public sealed class SqlQueryExecutor(
    IDatabaseConnectionManager connectionManager,
    ILogger<SqlQueryExecutor> logger
) : ISqlQueryExecutor
{
    private readonly IDatabaseConnectionManager _connectionManager = connectionManager;
    private readonly ILogger<SqlQueryExecutor> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Data.Resources.LogMessages", typeof(SqlQueryExecutor).Assembly);

    public async Task<SqlDataReader> ExecuteReaderAsync(string query, Dictionary<string, object>? parameters = null)
    {
        var connection = await _connectionManager.GetOpenConnectionAsync().ConfigureAwait(false);
        using var command = connection.CreateCommand();

        command.CommandText = query;
        _logger.LogDebug(_resourceManagerLogMessages.GetString("ExecutingSQLQuery"), query);

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }
}
