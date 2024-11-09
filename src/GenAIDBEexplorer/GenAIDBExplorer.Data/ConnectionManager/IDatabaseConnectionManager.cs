using Microsoft.Data.SqlClient;

namespace GenAIDBExplorer.Data.ConnectionManager;

public interface IDatabaseConnectionManager : IDisposable
{
    Task<SqlConnection> GetOpenConnectionAsync();
}
