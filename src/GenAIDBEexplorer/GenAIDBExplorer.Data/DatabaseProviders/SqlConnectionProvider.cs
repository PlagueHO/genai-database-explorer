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
            this._project.Settings.Database.ConnectionString ??
            throw new InvalidDataException($"Missing database connection string.");

        var connection = new SqlConnection(connectionString);

        try
        {
            _logger.LogInformation("Opening SQL connection to {ConnectionString}", connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            _logger.LogInformation("SQL connection opened successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open SQL connection to {ConnectionString}", connectionString);
            connection.Dispose();
            throw;
        }

        return connection;
    }
}
