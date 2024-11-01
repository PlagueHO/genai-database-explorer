using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Data.DatabaseProviders
{
    public interface IDatabaseConnectionProviderFactory
    {
        Func<IServiceProvider, SqlConnectionProvider> CreateSqlProvider(IProject project);
    }
}
