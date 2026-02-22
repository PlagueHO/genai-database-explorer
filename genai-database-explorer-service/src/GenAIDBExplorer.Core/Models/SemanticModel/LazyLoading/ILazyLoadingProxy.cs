using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Core.Models.SemanticModel.LazyLoading;

/// <summary>
/// Interface for lazy loading proxy objects that defer entity loading until accessed.
/// </summary>
/// <typeparam name="T">The type of entity to lazy load.</typeparam>
public interface ILazyLoadingProxy<T> : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the entities have been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the loaded entities. Triggers loading if not already loaded.
    /// </summary>
    Task<IEnumerable<T>> GetEntitiesAsync();

    /// <summary>
    /// Forces the entities to be loaded immediately.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Resets the proxy to an unloaded state.
    /// </summary>
    void Reset();
}
