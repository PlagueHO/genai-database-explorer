using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Resources;

namespace GenAIDBExplorer.Core.Data.ConnectionManager
{
    /// <summary>
    /// Manages the database connection lifecycle, ensuring that a single open connection is maintained.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing the lifecycle of a SQL database connection.
    /// It ensures that a single open connection is maintained and reused, improving efficiency
    /// and reducing the overhead of repeatedly opening and closing connections.
    /// </remarks>
    public sealed class DatabaseConnectionManager(
        IDatabaseConnectionProvider connectionProvider,
        ILogger<DatabaseConnectionManager> logger
    ) : IDatabaseConnectionManager
    {
        private readonly IDatabaseConnectionProvider _connectionProvider = connectionProvider;
        private readonly ILogger<DatabaseConnectionManager> _logger = logger;
        private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SchemaRepository).Assembly);
        private SqlConnection? _connection;
        private bool _disposed = false;

        /// <summary>
        /// Gets an open SQL connection, opening a new connection if necessary.
        /// </summary>
        /// <returns>An open <see cref="SqlConnection"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the connection could not be opened.</exception>
        /// <remarks>
        /// This method checks if there is an existing open connection. If not, it uses the
        /// <see cref="IDatabaseConnectionProvider"/> to open a new connection. The connection is then reused for subsequent requests.
        /// </remarks>
        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = await _connectionProvider.ConnectAsync().ConfigureAwait(false);

                if (_connection.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException(_resourceManagerErrorMessages.GetString("ErrorConnectingToDatabase"));
                }
            }

            return _connection;
        }

        /// <summary>
        /// Disposes the managed resources.
        /// </summary>
        /// <remarks>
        /// This method disposes the managed resources, specifically the SQL connection, if it is open.
        /// It ensures that the connection is properly closed and disposed of when the
        /// <see cref="DatabaseConnectionManager"/> is no longer needed.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to dispose managed resources.</param>
        /// <remarks>
        /// This method disposes the managed resources, specifically the SQL connection, if it is open.
        /// It ensures that the connection is properly closed and disposed of when the
        /// <see cref="DatabaseConnectionManager"/> is no longer needed.
        /// </remarks>
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

        /// <summary>
        /// Finalizer for the <see cref="DatabaseConnectionManager"/> class.
        /// </summary>
        /// <remarks>
        /// This finalizer ensures that the managed resources are properly disposed of when the
        /// <see cref="DatabaseConnectionManager"/> is garbage collected.
        /// </remarks>
        ~DatabaseConnectionManager()
        {
            Dispose(false);
        }
    }
}