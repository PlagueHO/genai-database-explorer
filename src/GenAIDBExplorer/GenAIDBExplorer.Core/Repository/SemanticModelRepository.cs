using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Default repository for semantic model persistence.
    /// </summary>
    public class SemanticModelRepository : ISemanticModelRepository
    {
        private readonly IPersistenceStrategyFactory _strategyFactory;
        private readonly ILogger<SemanticModelRepository>? _logger;

        public SemanticModelRepository(
            IPersistenceStrategyFactory strategyFactory,
            ILogger<SemanticModelRepository>? logger = null)
        {
            _strategyFactory = strategyFactory;
            _logger = logger;
        }

        public Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            return strategy.SaveModelAsync(model, modelPath);
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null)
        {
            return LoadModelAsync(modelPath, enableLazyLoading: false, strategyName);
        }

        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            var model = await strategy.LoadModelAsync(modelPath);

            if (enableLazyLoading)
            {
                _logger?.LogDebug("Enabling lazy loading for semantic model at {ModelPath}", modelPath.FullName);
                model.EnableLazyLoading(modelPath, strategy);
            }

            return model;
        }
    }
}
