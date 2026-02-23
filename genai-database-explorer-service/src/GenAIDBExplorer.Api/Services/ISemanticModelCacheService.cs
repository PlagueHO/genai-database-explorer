using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Api.Services;

/// <summary>
/// Provides thread-safe in-memory caching of the semantic model for the API layer.
/// </summary>
public interface ISemanticModelCacheService
{
    /// <summary>
    /// Gets the currently cached semantic model, loading it if not yet available.
    /// </summary>
    Task<SemanticModel> GetModelAsync();

    /// <summary>
    /// Reloads the semantic model from the persistence layer, atomically swapping the cached reference.
    /// </summary>
    Task<SemanticModel> ReloadModelAsync();

    /// <summary>
    /// Gets a value indicating whether the semantic model has been loaded into the cache.
    /// </summary>
    bool IsLoaded { get; }
}
