using System;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Factory for selecting a persistence strategy.
    /// </summary>
    public interface IPersistenceStrategyFactory
    {
        /// <summary>
        /// Returns the named persistence strategy, or the default if none specified.
        /// </summary>
        /// <param name="strategyName">The strategy name.</param>
        /// <returns>The persistence strategy instance.</returns>
        ISemanticModelPersistenceStrategy GetStrategy(string? strategyName = null);
    }
}
