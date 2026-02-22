using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Models.SemanticModel.LazyLoading;

/// <summary>
/// Basic lazy loading proxy implementation that defers entity loading until accessed.
/// </summary>
/// <typeparam name="T">The type of entity to lazy load.</typeparam>
public sealed class LazyLoadingProxy<T> : ILazyLoadingProxy<T>
{
    private readonly Func<Task<IEnumerable<T>>> _loadFunction;
    private readonly ILogger<LazyLoadingProxy<T>>? _logger;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    private IEnumerable<T>? _entities;
    private bool _isLoaded;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyLoadingProxy{T}"/> class.
    /// </summary>
    /// <param name="loadFunction">Function to load entities when needed.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public LazyLoadingProxy(
        Func<Task<IEnumerable<T>>> loadFunction,
        ILogger<LazyLoadingProxy<T>>? logger = null)
    {
        _loadFunction = loadFunction ?? throw new ArgumentNullException(nameof(loadFunction));
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsLoaded => _isLoaded;

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetEntitiesAsync()
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            await LoadAsync().ConfigureAwait(false);
        }

        return _entities ?? Enumerable.Empty<T>();
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        ThrowIfDisposed();

        if (_isLoaded)
        {
            return;
        }

        await _loadSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check pattern to avoid race conditions
            if (_isLoaded)
            {
                return;
            }

            _logger?.LogDebug("Loading entities for type {EntityType}", typeof(T).Name);

            _entities = await _loadFunction().ConfigureAwait(false);
            _isLoaded = true;

            _logger?.LogDebug("Successfully loaded {EntityCount} entities for type {EntityType}",
                _entities?.Count() ?? 0, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load entities for type {EntityType}", typeof(T).Name);
            throw;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        ThrowIfDisposed();

        _loadSemaphore.Wait();
        try
        {
            _entities = null;
            _isLoaded = false;

            _logger?.LogDebug("Reset lazy loading proxy for type {EntityType}", typeof(T).Name);
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _loadSemaphore?.Dispose();
        _entities = null;
        _disposed = true;

        _logger?.LogDebug("Disposed lazy loading proxy for type {EntityType}", typeof(T).Name);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LazyLoadingProxy<T>));
        }
    }
}
