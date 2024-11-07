using Microsoft.Data.SqlClient;
using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation("Opening SQL connection to {ConnectionString}", connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            _logger.LogInformation("SQL connection opened successfully.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL exception occurred while opening connection to {ConnectionString}", connectionString);
            connection.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while opening connection to {ConnectionString}", connectionString);
            connection.Dispose();
            throw;
        }

        // log the connection state
        _logger.LogInformation("Connection state: {ConnectionState}", connection.State);

        return connection;
    }
}
