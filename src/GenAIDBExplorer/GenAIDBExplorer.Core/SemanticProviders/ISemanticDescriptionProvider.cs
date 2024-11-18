using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Provides functionality to generate semantic descriptions for semantic model entities.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public interface ISemanticDescriptionProvider
{
    Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelTable table);
    Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelView view);
    Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
    Task<List<TableInfo>> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view);
}
