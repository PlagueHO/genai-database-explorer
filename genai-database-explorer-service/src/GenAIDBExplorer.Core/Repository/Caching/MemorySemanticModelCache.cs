using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAIDBExplorer.Core.Repository.Caching;

/// <summary>
/// Memory-based implementation of semantic model cache with statistics tracking.
/// </summary>
public class MemorySemanticModelCache : ISemanticModelCache, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemorySemanticModelCache> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cacheAccessTimes;
    private readonly Timer? _compactionTimer;
    private long _totalRequests;
    private long _cacheHits;
    private long _cacheMisses;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemorySemanticModelCache"/> class.
    /// </summary>
    /// <param name="memoryCache">The underlying memory cache implementation.</param>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public MemorySemanticModelCache(
        IMemoryCache memoryCache,
        IOptions<CacheOptions> options,
        ILogger<MemorySemanticModelCache> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheAccessTimes = new ConcurrentDictionary<string, DateTime>();

        // Initialize compaction timer if enabled
        if (_options.CompactionInterval > TimeSpan.Zero)
        {
            _compactionTimer = new Timer(
                CompactExpiredEntries,
                null,
                _options.CompactionInterval,
                _options.CompactionInterval);
        }

        _logger.LogInformation("MemorySemanticModelCache initialized with max size: {MaxSize}, default expiration: {Expiration}",
            _options.MaxCacheSize, _options.DefaultExpiration);
    }

    /// <inheritdoc />
    public Task<SemanticModel?> GetAsync(string cacheKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        if (_options.EnableStatistics)
        {
            Interlocked.Increment(ref _totalRequests);
        }

        try
        {
            if (_memoryCache.TryGetValue(cacheKey, out var cachedModel) && cachedModel is SemanticModel model)
            {
                if (_options.EnableStatistics)
                {
                    Interlocked.Increment(ref _cacheHits);
                    _cacheAccessTimes.TryAdd(cacheKey, DateTime.UtcNow);
                }

                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return Task.FromResult<SemanticModel?>(model);
            }

            if (_options.EnableStatistics)
            {
                Interlocked.Increment(ref _cacheMisses);
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            return Task.FromResult<SemanticModel?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item from cache with key: {CacheKey}", cacheKey);
            return Task.FromResult<SemanticModel?>(null);
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string cacheKey, SemanticModel model, TimeSpan? expiration = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            var expirationTime = expiration ?? _options.DefaultExpiration;
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime,
                Priority = CacheItemPriority.Normal,
                Size = EstimateModelSize(model)
            };

            // Add removal callback to clean up tracking data
            cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    if (key is string stringKey)
                    {
                        _cacheAccessTimes.TryRemove(stringKey, out _);
                        _logger.LogDebug("Cache entry evicted: {CacheKey}, Reason: {Reason}", stringKey, reason);
                    }
                }
            });

            _memoryCache.Set(cacheKey, model, cacheEntryOptions);

            if (_options.EnableStatistics)
            {
                _cacheAccessTimes.TryAdd(cacheKey, DateTime.UtcNow);
            }

            _logger.LogDebug("Added item to cache with key: {CacheKey}, Expiration: {Expiration}", cacheKey, expirationTime);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cache with key: {CacheKey}", cacheKey);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string cacheKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        try
        {
            var existed = _memoryCache.TryGetValue(cacheKey, out _);
            _memoryCache.Remove(cacheKey);
            _cacheAccessTimes.TryRemove(cacheKey, out _);

            _logger.LogDebug("Removed item from cache with key: {CacheKey}, Existed: {Existed}", cacheKey, existed);
            return Task.FromResult(existed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cache with key: {CacheKey}", cacheKey);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Clear all entries by removing known keys
            var keysToRemove = new List<string>(_cacheAccessTimes.Keys);
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }

            _cacheAccessTimes.Clear();
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);

            _logger.LogInformation("Cache cleared successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<CacheStatistics> GetStatisticsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var totalRequests = Interlocked.Read(ref _totalRequests);
            var cacheHits = Interlocked.Read(ref _cacheHits);
            var cacheMisses = Interlocked.Read(ref _cacheMisses);

            var hitRate = totalRequests > 0 ? (double)cacheHits / totalRequests : 0.0;
            var cacheSize = _cacheAccessTimes.Count;

            // Estimate memory usage (simplified calculation)
            var estimatedMemoryUsage = cacheSize * 1024L; // Rough estimate: 1KB per cache entry metadata

            var statistics = new CacheStatistics(
                totalRequests,
                cacheHits,
                cacheMisses,
                hitRate,
                cacheSize,
                estimatedMemoryUsage);

            // Log warning if hit rate is below threshold
            if (_options.EnableStatistics && hitRate < _options.HitRateThreshold && totalRequests > 10)
            {
                _logger.LogWarning(
                    "Cache hit rate ({HitRate:P2}) is below threshold ({Threshold:P2}). Consider adjusting cache configuration.",
                    hitRate, _options.HitRateThreshold);
            }

            return Task.FromResult(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return Task.FromResult(new CacheStatistics(0, 0, 0, 0.0, 0, 0));
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string cacheKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        try
        {
            var exists = _memoryCache.TryGetValue(cacheKey, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {CacheKey}", cacheKey);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Estimates the memory size of a semantic model for cache sizing.
    /// </summary>
    /// <param name="model">The semantic model to estimate.</param>
    /// <returns>Estimated size in bytes.</returns>
    private static long EstimateModelSize(SemanticModel model)
    {
        // Simplified size estimation - in real implementation, this could be more sophisticated
        var baseSize = 1024L; // Base overhead per model
        var tableSize = (model.Tables?.Count ?? 0) * 512L; // Estimate per table
        var viewSize = (model.Views?.Count ?? 0) * 256L; // Estimate per view
        var procedureSize = (model.StoredProcedures?.Count ?? 0) * 256L; // Estimate per procedure

        return baseSize + tableSize + viewSize + procedureSize;
    }

    /// <summary>
    /// Compacts expired entries from the cache access tracking.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void CompactExpiredEntries(object? state)
    {
        if (_disposed)
            return;

        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(_options.DefaultExpiration);
            var keysToRemove = new List<string>();

            foreach (var kvp in _cacheAccessTimes)
            {
                if (kvp.Value < cutoffTime && !_memoryCache.TryGetValue(kvp.Key, out _))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cacheAccessTimes.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Compacted {Count} expired cache tracking entries", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache compaction");
        }
    }

    /// <summary>
    /// Disposes the cache and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the cache and releases all resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _logger.LogDebug("Disposing MemorySemanticModelCache");

            _compactionTimer?.Dispose();
            _cacheAccessTimes.Clear();

            _disposed = true;
            _logger.LogDebug("MemorySemanticModelCache disposed");
        }
    }
}
