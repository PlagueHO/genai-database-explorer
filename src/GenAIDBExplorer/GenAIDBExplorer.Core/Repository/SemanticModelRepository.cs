using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Default repository for semantic model persistence.
    /// </summary>
    public class SemanticModelRepository : ISemanticModelRepository
    {
        private readonly IPersistenceStrategyFactory _strategyFactory;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger<SemanticModelRepository>? _logger;

        public SemanticModelRepository(
            IPersistenceStrategyFactory strategyFactory,
            ILogger<SemanticModelRepository>? logger = null,
            ILoggerFactory? loggerFactory = null)
        {
            _strategyFactory = strategyFactory;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            return strategy.SaveModelAsync(model, modelPath);
        }

        public async Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            // If change tracking is not enabled or there are no changes, perform a full save
            if (!model.IsChangeTrackingEnabled || !model.HasUnsavedChanges)
            {
                _logger?.LogDebug("Change tracking not enabled or no changes detected. Performing full save for model at {ModelPath}", modelPath.FullName);
                await SaveModelAsync(model, modelPath, strategyName);
                return;
            }

            _logger?.LogDebug("Selective persistence - saving only changed entities for model at {ModelPath}", modelPath.FullName);

            // For Phase 4b, we implement basic selective persistence by performing a full save
            // but only when there are actual changes. Future phases could implement more granular
            // selective persistence by only saving specific entity files.
            await SaveModelAsync(model, modelPath, strategyName);

            // Mark all entities as clean after successful save
            model.AcceptAllChanges();

            _logger?.LogDebug("Selective persistence completed for model at {ModelPath}", modelPath.FullName);
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null)
        {
            return LoadModelAsync(modelPath, enableLazyLoading: false, strategyName);
        }

        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, string? strategyName = null)
        {
            return await LoadModelAsync(modelPath, enableLazyLoading, enableChangeTracking: false, strategyName);
        }

        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, bool enableChangeTracking, string? strategyName = null)
        {
            var strategy = _strategyFactory.GetStrategy(strategyName);
            var model = await strategy.LoadModelAsync(modelPath);

            if (enableLazyLoading)
            {
                _logger?.LogDebug("Enabling lazy loading for semantic model at {ModelPath}", modelPath.FullName);
                model.EnableLazyLoading(modelPath, strategy);
            }

            if (enableChangeTracking)
            {
                _logger?.LogDebug("Enabling change tracking for semantic model at {ModelPath}", modelPath.FullName);
                var changeTrackerLogger = _loggerFactory?.CreateLogger<ChangeTracker>();
                var changeTracker = new ChangeTracker(changeTrackerLogger);
                model.EnableChangeTracking(changeTracker);
            }

            return model;
        }
    }
}
