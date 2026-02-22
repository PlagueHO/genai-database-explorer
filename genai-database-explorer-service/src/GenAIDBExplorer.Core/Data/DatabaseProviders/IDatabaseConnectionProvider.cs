using Microsoft.Data.SqlClient;

namespace GenAIDBExplorer.Core.Data.DatabaseProviders;

/// <summary>
/// Interface for database connection providers.
/// </summary>
public interface IDatabaseConnectionProvider
{
    /// <summary>
    /// Factory method for producing a live SQL connection instance.
    /// </summary>
    /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
    Task<SqlConnection> ConnectAsync();
}