using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public interface ISemanticModelProvider
{
    Task<SemanticModel> GetSchemaAsync(string? description, params string[] tableNames);
    IAsyncEnumerable<SemanticModelTable> QueryTablesAsync();
    Task<Dictionary<string, (string table, string column)>> QueryReferencesAsync();
    Task<Dictionary<string, string?>> QueryTableDescriptionsAsync();
}
