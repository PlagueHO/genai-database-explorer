using System;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Selects and returns persistence strategy implementations.
    /// </summary>
    public class PersistenceStrategyFactory : IPersistenceStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, ISemanticModelPersistenceStrategy> _strategies;

        public PersistenceStrategyFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _strategies = new Dictionary<string, ISemanticModelPersistenceStrategy>(StringComparer.OrdinalIgnoreCase);
        }

        public ISemanticModelPersistenceStrategy GetStrategy(string? strategyName = null)
        {
            var name = strategyName ?? _configuration["PersistenceStrategy"] ?? "LocalDisk";

            // Lazy-load strategies only when requested
            if (!_strategies.TryGetValue(name, out var strategy))
            {
                strategy = name.ToLowerInvariant() switch
                {
                    "localdisk" => _serviceProvider.GetRequiredService<ILocalDiskPersistenceStrategy>(),
                    "azureblob" => _serviceProvider.GetRequiredService<IAzureBlobPersistenceStrategy>(),
                    "cosmosdb" => _serviceProvider.GetRequiredService<ICosmosDbPersistenceStrategy>(),
                    _ => throw new ArgumentException($"Persistence strategy '{name}' is not supported.", nameof(strategyName))
                };

                _strategies[name] = strategy;
            }

            return strategy;
        }
    }
}
