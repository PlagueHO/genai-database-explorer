using GenAIDBExplorer.Models.SemanticModel;
using System.Threading.Tasks;

namespace GenAIDBExplorer.AI.SemanticProviders;

/// <summary>
/// Provides functionality to generate semantic descriptions for semantic model entities.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public interface ISemanticDescriptionProvider
{
    Task UpdateSemanticDescriptionAsync(SemanticModelTable table);
}
