using GenAIDBExplorer.Data.DatabaseProviders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GenAIDBExplorer.Data.ConnectionManager
{
    public sealed class DatabaseConnectionManager(
        IDatabaseConnectionProvider connectionProvider,
        ILogger<DatabaseConnectionManager> logger
    ) : IDatabaseConnectionManager
    {
        private readonly IDatabaseConnectionProvider _connectionProvider = connectionProvider;
        private readonly ILogger<DatabaseConnectionManager> _logger = logger;
        private SqlConnection? _connection;
        private bool _disposed = false;

        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _logger.LogInformation("Connecting to the database...");
                _connection = await _connectionProvider.ConnectAsync().ConfigureAwait(false);

                if (_connection.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Failed to open the database connection.");
                }
            }

            return _connection;
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

        ~DatabaseConnectionManager()
        {
            Dispose(false);
        }
    }
}