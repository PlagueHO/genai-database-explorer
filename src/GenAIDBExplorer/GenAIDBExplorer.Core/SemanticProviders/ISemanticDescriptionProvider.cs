using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Provides functionality to generate semantic descriptions for semantic model entities.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public interface ISemanticDescriptionProvider
{
    Task<SemanticProcessSummary> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel);
    Task<SemanticProcessSummary> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, TableList tables);
    Task<SemanticProcessSummary> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelTable table);
    Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel);
    Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, ViewList views);
    Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelView view);
    Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel);
    Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, StoredProcedureList storedProcedures);
    Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
    Task<TableList> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view);
    Task<TableList> GetTableListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
    Task<ViewList> GetViewListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure);
}
