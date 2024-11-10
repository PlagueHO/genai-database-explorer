using System.Collections.Generic;
using System.Threading.Tasks;
using GenAIDBExplorer.Models.SemanticModel;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public interface ISchemaRepository
{
    Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null);
    Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null);
    Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table);
    Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view);
    Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table);
}
