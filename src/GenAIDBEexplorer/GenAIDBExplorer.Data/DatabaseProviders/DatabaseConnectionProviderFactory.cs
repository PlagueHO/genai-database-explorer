using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Data.DatabaseProviders
{
    public class DatabaseConnectionProviderFactory : IDatabaseConnectionProviderFactory
    {
        public Func<IServiceProvider, SqlConnectionProvider> CreateSqlProvider(IProject project)
        {
            return CreateProvider;

            SqlConnectionProvider CreateProvider(IServiceProvider provider)
            {
                return new SqlConnectionProvider(project);
            }
        }
    }
}
