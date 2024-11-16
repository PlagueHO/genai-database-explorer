using Microsoft.Data.SqlClient;
using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.SemanticModelProviders;
using System.Resources;

namespace GenAIDBExplorer.Data.DatabaseProviders;

/// <summary>
/// Responsible for producing a connection string for the requested project.
/// </summary>
public sealed class SqlConnectionProvider(
    IProject project,
    ILogger<SqlConnectionProvider> logger
) : IDatabaseConnectionProvider
{
    private readonly IProject _project = project;
    private readonly ILogger<SqlConnectionProvider> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Data.Resources.LogMessages", typeof(SqlConnectionProvider).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Data.Resources.ErrorMessages", typeof(SqlConnectionProvider).Assembly);


    /// <summary>
    /// Factory method for producing a live SQL connection instance.
    /// </summary>
    /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
    /// <remarks>
    /// Connection pooling enabled by default makes re-establishing connections
    /// relatively efficient.
    /// </remarks>
    public async Task<SqlConnection> ConnectAsync()
    {
        var connectionString =
            _project.Settings.Database.ConnectionString ??
                throw new InvalidDataException($"Missing database connection string.");

        var connection = new SqlConnection(connectionString);

        try
        {
            _logger.LogInformation(_resourceManagerLogMessages.GetString("ConnectingSQLDatabase"));
            await connection.OpenAsync().ConfigureAwait(false);
            _logger.LogInformation(_resourceManagerLogMessages.GetString("ConnectSQLSuccessful"));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, _resourceManagerErrorMessages.GetString("ErrorConnectingToDatabaseSQL"));
            connection.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _resourceManagerErrorMessages.GetString("ErrorConnectingToDatabase"));
            connection.Dispose();
            throw;
        }

        // log the connection state
        _logger.LogInformation(_resourceManagerLogMessages.GetString("DatabaseConnectionState"), connection.State);

        return connection;
    }
}
