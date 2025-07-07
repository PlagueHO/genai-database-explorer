using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using GenAIDBExplorer.Core.Repository.Caching;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Default repository for semantic model persistence with concurrent operation protection.
    /// </summary>
    public class SemanticModelRepository : ISemanticModelRepository, IDisposable
    {
        private readonly IPersistenceStrategyFactory _strategyFactory;
        private readonly ISemanticModelCache? _cache;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger<SemanticModelRepository>? _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _pathSemaphores;
        private readonly SemaphoreSlim _globalSemaphore;
        private readonly int _maxConcurrentOperations;
        private bool _disposed;

        public SemanticModelRepository(
            IPersistenceStrategyFactory strategyFactory,
            ILogger<SemanticModelRepository>? logger = null,
            ILoggerFactory? loggerFactory = null,
            ISemanticModelCache? cache = null,
            int maxConcurrentOperations = 10)
        {
            _strategyFactory = strategyFactory;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _cache = cache;
            _maxConcurrentOperations = maxConcurrentOperations;
            _pathSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _globalSemaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
        }

        public async Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var sanitizedPath = await ValidateAndSanitizePathAsync(modelPath);
            await ExecuteWithConcurrencyProtectionAsync(sanitizedPath, async () =>
            {
                _logger?.LogDebug("Saving semantic model to {ModelPath}", sanitizedPath);
                var strategy = _strategyFactory.GetStrategy(strategyName);
                await strategy.SaveModelAsync(model, new DirectoryInfo(sanitizedPath));
            });
        }

        public async Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var sanitizedPath = await ValidateAndSanitizePathAsync(modelPath);

            // If change tracking is not enabled or there are no changes, perform a full save
            if (!model.IsChangeTrackingEnabled || !model.HasUnsavedChanges)
            {
                _logger?.LogDebug("Change tracking not enabled or no changes detected. Performing full save for model at {ModelPath}", sanitizedPath);
                await SaveModelAsync(model, new DirectoryInfo(sanitizedPath), strategyName);
                return;
            }

            await ExecuteWithConcurrencyProtectionAsync(sanitizedPath, async () =>
            {
                _logger?.LogDebug("Selective persistence - saving only changed entities for model at {ModelPath}", sanitizedPath);

                // For Phase 4b, we implement basic selective persistence by performing a full save
                // but only when there are actual changes. Future phases could implement more granular
                // selective persistence by only saving specific entity files.
                var strategy = _strategyFactory.GetStrategy(strategyName);
                await strategy.SaveModelAsync(model, new DirectoryInfo(sanitizedPath));

                // Mark all entities as clean after successful save
                model.AcceptAllChanges();

                _logger?.LogDebug("Selective persistence completed for model at {ModelPath}", sanitizedPath);
            });
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
            return await LoadModelAsync(modelPath, enableLazyLoading, enableChangeTracking, enableCaching: false, strategyName);
        }

        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, bool enableChangeTracking, bool enableCaching, string? strategyName = null)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var sanitizedPath = await ValidateAndSanitizePathAsync(modelPath);

            return await ExecuteWithConcurrencyProtectionAsync(sanitizedPath, async () =>
            {
                SemanticModel? model = null;
                string? cacheKey = null;
                var strategy = _strategyFactory.GetStrategy(strategyName);

                // Try to load from cache if caching is enabled and cache is available
                if (enableCaching && _cache != null)
                {
                    cacheKey = GenerateCacheKey(sanitizedPath, strategyName);
                    _logger?.LogDebug("Attempting to load model from cache with key: {CacheKey}", cacheKey);

                    try
                    {
                        model = await _cache.GetAsync(cacheKey);
                        if (model != null)
                        {
                            _logger?.LogDebug("Model loaded from cache for path: {ModelPath}", sanitizedPath);

                            // Apply lazy loading and change tracking to cached model if requested
                            if (enableLazyLoading && !model.IsLazyLoadingEnabled)
                            {
                                model.EnableLazyLoading(new DirectoryInfo(sanitizedPath), strategy);
                            }

                            if (enableChangeTracking && !model.IsChangeTrackingEnabled)
                            {
                                var changeTrackerLogger = _loggerFactory?.CreateLogger<ChangeTracker>();
                                var changeTracker = new ChangeTracker(changeTrackerLogger);
                                model.EnableChangeTracking(changeTracker);
                            }

                            return model;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to load model from cache, continuing with persistence load");
                        model = null; // Ensure model is null if cache read failed
                    }
                }

                // Load from persistence if not found in cache
                _logger?.LogDebug("Loading semantic model from persistence at {ModelPath}", sanitizedPath);

                model = await strategy.LoadModelAsync(new DirectoryInfo(sanitizedPath));

                if (enableLazyLoading)
                {
                    _logger?.LogDebug("Enabling lazy loading for semantic model at {ModelPath}", sanitizedPath);
                    model.EnableLazyLoading(new DirectoryInfo(sanitizedPath), strategy);
                }

                if (enableChangeTracking)
                {
                    _logger?.LogDebug("Enabling change tracking for semantic model at {ModelPath}", sanitizedPath);
                    var changeTrackerLogger = _loggerFactory?.CreateLogger<ChangeTracker>();
                    var changeTracker = new ChangeTracker(changeTrackerLogger);
                    model.EnableChangeTracking(changeTracker);
                }

                // Store in cache if caching is enabled and cache is available
                if (enableCaching && _cache != null && cacheKey != null)
                {
                    _logger?.LogDebug("Storing model in cache with key: {CacheKey}", cacheKey);
                    try
                    {
                        await _cache.SetAsync(cacheKey, model);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to store model in cache, continuing without caching");
                    }
                }

                return model;
            });
        }

        /// <summary>
        /// Validates and sanitizes a path for security.
        /// </summary>
        /// <param name="modelPath">The path to validate.</param>
        /// <returns>The sanitized path.</returns>
        private Task<string> ValidateAndSanitizePathAsync(DirectoryInfo modelPath)
        {
            try
            {
                // Validate input security
                EntityNameSanitizer.ValidateInputSecurity(modelPath.FullName, nameof(modelPath));

                // Validate and sanitize the path
                var sanitizedPath = PathValidator.ValidateAndSanitizePath(modelPath.FullName);

                // Ensure the path is safe for concurrent operations
                if (!PathValidator.IsPathSafeForConcurrentOperations(sanitizedPath))
                {
                    throw new InvalidOperationException($"Path is not safe for concurrent operations: {sanitizedPath}");
                }

                _logger?.LogDebug("Path validated and sanitized: {OriginalPath} -> {SanitizedPath}",
                    modelPath.FullName, sanitizedPath);

                return Task.FromResult(sanitizedPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Path validation failed for {ModelPath}", modelPath.FullName);
                throw new ArgumentException($"Invalid or unsafe path: {modelPath.FullName}", nameof(modelPath), ex);
            }
        }

        /// <summary>
        /// Generates a cache key for a semantic model based on its path and strategy.
        /// </summary>
        /// <param name="sanitizedPath">The sanitized model path.</param>
        /// <param name="strategyName">The optional strategy name.</param>
        /// <returns>A unique cache key.</returns>
        private static string GenerateCacheKey(string sanitizedPath, string? strategyName)
        {
            // Create a deterministic cache key using the path and strategy
            var keySource = $"{sanitizedPath}|{strategyName ?? "default"}";

            // Use SHA256 to create a deterministic hash for the cache key
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Prefix with a recognizable identifier and include first few chars of original path for debugging
            var pathSegment = Path.GetFileName(sanitizedPath) ?? "unknown";
            return $"semantic_model_{pathSegment}_{hashString[..16]}";
        }

        /// <summary>
        /// Executes an operation with concurrency protection.
        /// </summary>
        /// <param name="path">The path for which to acquire the lock.</param>
        /// <param name="operation">The operation to execute.</param>
        private async Task ExecuteWithConcurrencyProtectionAsync(string path, Func<Task> operation)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // First acquire global semaphore to limit total concurrent operations
            await _globalSemaphore.WaitAsync();
            try
            {
                // Then acquire path-specific semaphore to prevent conflicts on the same path
                var pathSemaphore = _pathSemaphores.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
                await pathSemaphore.WaitAsync();
                try
                {
                    _logger?.LogDebug("Acquired concurrent operation lock for path: {Path}", path);
                    await operation();
                }
                finally
                {
                    pathSemaphore.Release();
                    _logger?.LogDebug("Released concurrent operation lock for path: {Path}", path);
                }
            }
            finally
            {
                _globalSemaphore.Release();
            }
        }

        /// <summary>
        /// Executes an operation with concurrency protection and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of result.</typeparam>
        /// <param name="path">The path for which to acquire the lock.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The operation result.</returns>
        private async Task<T> ExecuteWithConcurrencyProtectionAsync<T>(string path, Func<Task<T>> operation)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // First acquire global semaphore to limit total concurrent operations
            await _globalSemaphore.WaitAsync();
            try
            {
                // Then acquire path-specific semaphore to prevent conflicts on the same path
                var pathSemaphore = _pathSemaphores.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
                await pathSemaphore.WaitAsync();
                try
                {
                    _logger?.LogDebug("Acquired concurrent operation lock for path: {Path}", path);
                    return await operation();
                }
                finally
                {
                    pathSemaphore.Release();
                    _logger?.LogDebug("Released concurrent operation lock for path: {Path}", path);
                }
            }
            finally
            {
                _globalSemaphore.Release();
            }
        }

        /// <summary>
        /// Disposes the repository and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the repository and releases all resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _logger?.LogDebug("Disposing SemanticModelRepository");

                _globalSemaphore?.Dispose();

                foreach (var semaphore in _pathSemaphores.Values)
                {
                    semaphore?.Dispose();
                }
                _pathSemaphores.Clear();

                _disposed = true;
                _logger?.LogDebug("SemanticModelRepository disposed");
            }
        }
    }
}
