using Microsoft.Data.SqlClient;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Data.DatabaseProviders;

/// <summary>
/// Responsible for producing a connection string for the requested project.
/// </summary>
public sealed class SqlConnectionProvider : IDatabaseConnectionProvider
{
    private readonly IProject _project;

    public SqlConnectionProvider(IProject project)
    {
        this._project = project;
    }

    /// <summary>
    /// Factory method for producing a live SQL connection instance.
    /// </summary>
    /// <param name="schemaName">The schema name (which should match a corresponding connectionstring setting).</param>
    /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
    /// <remarks>
    /// Connection pooling enabled by default makes re-establishing connections
    /// relatively efficient.
    /// </remarks>
    public async Task<SqlConnection> ConnectAsync(string schemaName)
    {
        var connectionString =
            this._project.DatabaseSettings.ConnectionString ??
            throw new InvalidDataException($"Missing configuration for connection-string: {schemaName}");

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
