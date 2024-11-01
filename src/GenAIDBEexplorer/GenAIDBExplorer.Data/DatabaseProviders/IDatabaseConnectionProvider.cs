using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GenAIDBExplorer.Data.DatabaseProviders
{
    /// <summary>
    /// Interface for database connection providers.
    /// </summary>
    public interface IDatabaseConnectionProvider
    {
        /// <summary>
        /// Factory method for producing a live SQL connection instance.
        /// </summary>
        /// <param name="schemaName">The schema name (which should match a corresponding connectionstring setting).</param>
        /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
        Task<SqlConnection> ConnectAsync(string schemaName);
    }
}