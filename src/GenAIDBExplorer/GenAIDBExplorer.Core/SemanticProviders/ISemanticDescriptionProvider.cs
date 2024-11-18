using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Provides functionality to generate semantic descriptions for semantic model entities.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public interface ISemanticDescriptionProvider
{
    Task UpdateSemanticDescriptionAsync(SemanticModelTable table);
    Task UpdateSemanticDescriptionAsync(SemanticModelView view);
    Task UpdateSemanticDescriptionAsync(SemanticModelStoredProcedure storedProcedure);
    Task<List<TableInfo>> GetTableListFromViewDefinitionAsync(SemanticModelView view);
}
