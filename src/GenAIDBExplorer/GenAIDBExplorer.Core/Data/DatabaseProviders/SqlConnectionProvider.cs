using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Resources;

namespace GenAIDBExplorer.Core.Data.DatabaseProviders;

/// <summary>
/// Responsible for producing a connection string for the requested project and establishing a SQL connection.
/// </summary>
/// <remarks>
/// This class is responsible for creating and opening a SQL connection using the connection string provided in the project settings. It handles the connection lifecycle, including logging connection attempts and errors.
/// </remarks>
public sealed class SqlConnectionProvider(
    IProject project,
    ILogger<SqlConnectionProvider> logger
) : IDatabaseConnectionProvider
{
    private readonly IProject _project = project;
    private readonly ILogger<SqlConnectionProvider> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SqlConnectionProvider).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SqlConnectionProvider).Assembly);

    /// <summary>
    /// Factory method for producing a live SQL connection instance.
    /// </summary>
    /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
    /// <remarks>
    /// This method retrieves the connection string from the project settings and attempts to open a SQL connection. It logs the connection attempt and any errors that occur.
    /// Connection pooling enabled by default makes re-establishing connections relatively efficient.
    /// </remarks>
    /// <exception cref="InvalidDataException">Thrown if the connection string is missing.</exception>
    /// <exception cref="SqlException">Thrown if there is an error connecting to the SQL database.</exception>
    /// <exception cref="Exception">Thrown if there is a general error connecting to the database.</exception>
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

        // Log the connection state
        _logger.LogInformation(_resourceManagerLogMessages.GetString("DatabaseConnectionState"), connection.State);

        return connection;
    }
}