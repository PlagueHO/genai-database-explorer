using System;
using System.Collections.Generic;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Configuration;

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
            _strategies = new Dictionary<string, ISemanticModelPersistenceStrategy>(StringComparer.OrdinalIgnoreCase)
            {
                { "LocalDisk", (ISemanticModelPersistenceStrategy)serviceProvider.GetService(typeof(ILocalDiskPersistenceStrategy))! },
                { "AzureBlob", (ISemanticModelPersistenceStrategy)serviceProvider.GetService(typeof(IAzureBlobPersistenceStrategy))! },
                { "Cosmos", (ISemanticModelPersistenceStrategy)serviceProvider.GetService(typeof(ICosmosPersistenceStrategy))! }
            };
        }

        public ISemanticModelPersistenceStrategy GetStrategy(string? strategyName = null)
        {
            var name = strategyName ?? _configuration["PersistenceStrategy"] ?? "LocalDisk";
            if (_strategies.TryGetValue(name, out var strategy))
            {
                return strategy;
            }

            throw new ArgumentException($"Persistence strategy '{name}' is not registered.", nameof(strategyName));
        }
    }
}
