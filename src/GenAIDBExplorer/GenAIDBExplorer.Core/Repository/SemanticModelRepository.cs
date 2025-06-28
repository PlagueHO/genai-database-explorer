using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Default repository for semantic model persistence.
    /// </summary>
    public class SemanticModelRepository : ISemanticModelRepository
    {
        private readonly IPersistenceStrategyFactory _strategyFactory;

        public SemanticModelRepository(IPersistenceStrategyFactory strategyFactory)
        {
            _strategyFactory = strategyFactory;
        }

        public Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            return strategy.SaveModelAsync(model, modelPath);
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            return strategy.LoadModelAsync(modelPath);
        }
    }
}
