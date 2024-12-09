using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Provides functionality to generate semantic descriptions for semantic model entities.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public interface ISemanticDescriptionProvider
{
    Task<SemanticProcessResult> UpdateSemanticDescriptionAsync<T>(
        SemanticModel semanticModel,
        IEnumerable<T> entities
    ) where T : SemanticModelEntity;

    Task<SemanticProcessResult> UpdateSemanticDescriptionAsync(
        SemanticModel semanticModel,
        SemanticModelEntity entity
    );

    Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel);
    Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, TableList tables);
    Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelTable table);
    Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel);
    Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, ViewList views);
    Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelView view);
    Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel);
    Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, StoredProcedureList storedProcedures);
    Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
    Task<TableList> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view);
    Task<TableList> GetTableListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
}
