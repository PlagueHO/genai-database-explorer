using Microsoft.Data.SqlClient;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Data.DatabaseProviders;

/// <summary>
/// Responsible for producing a connection string for the requested project.
/// </summary>
public sealed class SqlConnectionProvider(IProject project) : IDatabaseConnectionProvider
{
    private readonly IProject _project = project;

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
            await connection.OpenAsync().ConfigureAwait(false);
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return connection;
    }
}
