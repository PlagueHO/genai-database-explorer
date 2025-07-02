using System;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository.Caching;

/// <summary>
/// Interface for semantic model caching operations.
/// </summary>
public interface ISemanticModelCache
{
    /// <summary>
    /// Gets a cached semantic model by its cache key.
    /// </summary>
    /// <param name="cacheKey">The unique cache key for the model.</param>
    /// <returns>The cached semantic model if found; otherwise, null.</returns>
    Task<SemanticModel?> GetAsync(string cacheKey);

    /// <summary>
    /// Sets a semantic model in the cache with the specified key.
    /// </summary>
    /// <param name="cacheKey">The unique cache key for the model.</param>
    /// <param name="model">The semantic model to cache.</param>
    /// <param name="expiration">Optional expiration time for the cached item.</param>
    Task SetAsync(string cacheKey, SemanticModel model, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a semantic model from the cache.
    /// </summary>
    /// <param name="cacheKey">The cache key to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(string cacheKey);

    /// <summary>
    /// Clears all cached semantic models.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets cache statistics including hit rate and cache size.
    /// </summary>
    /// <returns>The current cache statistics.</returns>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Checks if a cache key exists in the cache.
    /// </summary>
    /// <param name="cacheKey">The cache key to check.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string cacheKey);
}

/// <summary>
/// Represents cache statistics for monitoring cache performance.
/// </summary>
public record CacheStatistics(
    long TotalRequests,
    long CacheHits,
    long CacheMisses,
    double HitRate,
    int CacheSize,
    long TotalMemoryUsage);
