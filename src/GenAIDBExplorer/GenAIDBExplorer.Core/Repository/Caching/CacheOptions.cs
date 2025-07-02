using System;

namespace GenAIDBExplorer.Core.Repository.Caching;

/// <summary>
/// Configuration options for semantic model caching.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Configuration section name for cache options.
    /// </summary>
    public const string SectionName = "SemanticModelCache";

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items to store in the cache.
    /// </summary>
    public int MaxCacheSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the memory size limit for the cache in megabytes.
    /// </summary>
    public long MemoryLimitMB { get; set; } = 512;

    /// <summary>
    /// Gets or sets the interval for compacting expired entries.
    /// </summary>
    public TimeSpan CompactionInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the hit rate threshold for cache effectiveness warnings.
    /// </summary>
    public double HitRateThreshold { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets whether to enable cache statistics collection.
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
}
