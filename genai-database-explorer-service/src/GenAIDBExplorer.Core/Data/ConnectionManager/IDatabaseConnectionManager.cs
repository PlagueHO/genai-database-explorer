using Microsoft.Data.SqlClient;

namespace GenAIDBExplorer.Core.Data.ConnectionManager;

public interface IDatabaseConnectionManager : IDisposable
{
    Task<SqlConnection> GetOpenConnectionAsync();
}
