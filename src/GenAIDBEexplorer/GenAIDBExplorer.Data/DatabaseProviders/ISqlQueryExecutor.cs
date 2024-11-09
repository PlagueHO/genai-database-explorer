using Microsoft.Data.SqlClient;

namespace GenAIDBExplorer.Data.DatabaseProviders;

public interface ISqlQueryExecutor
{
    Task<SqlDataReader> ExecuteReaderAsync(string query, Dictionary<string, object>? parameters = null);
}
