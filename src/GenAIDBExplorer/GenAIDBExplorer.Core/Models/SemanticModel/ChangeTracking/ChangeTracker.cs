using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;

/// <summary>
/// Default implementation of change tracking for semantic model entities.
/// Provides thread-safe tracking of entity modifications.
/// </summary>
public sealed class ChangeTracker : IChangeTracker, IDisposable
{
    private readonly ConcurrentDictionary<object, bool> _entityStates;
    private readonly ILogger<ChangeTracker>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ChangeTracker class.
    /// </summary>
    /// <param name="logger">Optional logger for tracking operations.</param>
    public ChangeTracker(ILogger<ChangeTracker>? logger = null)
    {
        _entityStates = new ConcurrentDictionary<object, bool>();
        _logger = logger;
    }

    /// <summary>
    /// Event that is raised when an entity's dirty state changes.
    /// </summary>
    public event EventHandler<EntityStateChangedEventArgs>? EntityStateChanged;

    /// <summary>
    /// Gets the count of dirty (modified) entities.
    /// </summary>
    public int DirtyEntityCount => _entityStates.Count(kvp => kvp.Value);

    /// <summary>
    /// Determines whether there are any dirty (modified) entities.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public bool HasChanges 
    { 
        get
        {
            ThrowIfDisposed();
            return _entityStates.Values.Any(isDirty => isDirty);
        }
    }

    /// <summary>
    /// Marks an entity as dirty (modified).
    /// </summary>
    /// <param name="entity">The entity that has been modified.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public void MarkAsDirty(object entity)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(entity);

        var wasAlreadyDirty = _entityStates.GetValueOrDefault(entity, false);
        _entityStates.AddOrUpdate(entity, true, (_, _) => true);

        if (!wasAlreadyDirty)
        {
            _logger?.LogDebug("Entity {EntityType} marked as dirty", entity.GetType().Name);
            OnEntityStateChanged(new EntityStateChangedEventArgs(entity, true));
        }
    }

    /// <summary>
    /// Marks an entity as clean (unmodified).
    /// </summary>
    /// <param name="entity">The entity to mark as clean.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public void MarkAsClean(object entity)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(entity);

        var wasAlreadyClean = !_entityStates.GetValueOrDefault(entity, false);
        _entityStates.AddOrUpdate(entity, false, (_, _) => false);

        if (!wasAlreadyClean)
        {
            _logger?.LogDebug("Entity {EntityType} marked as clean", entity.GetType().Name);
            OnEntityStateChanged(new EntityStateChangedEventArgs(entity, false));
        }
    }

    /// <summary>
    /// Determines whether the specified entity has been modified.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>true if the entity is dirty (modified); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public bool IsDirty(object entity)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(entity);

        return _entityStates.GetValueOrDefault(entity, false);
    }

    /// <summary>
    /// Gets all dirty (modified) entities.
    /// </summary>
    /// <returns>A collection of all dirty entities.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public IEnumerable<object> GetDirtyEntities()
    {
        ThrowIfDisposed();

        return _entityStates
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList(); // Materialize to avoid issues with concurrent modification
    }

    /// <summary>
    /// Clears all tracked entities and resets the change tracker.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public void Clear()
    {
        ThrowIfDisposed();

        var clearedCount = _entityStates.Count;
        _entityStates.Clear();

        _logger?.LogDebug("Change tracker cleared. Removed {Count} tracked entities", clearedCount);
    }

    /// <summary>
    /// Accepts all changes and marks all entities as clean.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the change tracker has been disposed.</exception>
    public void AcceptAllChanges()
    {
        ThrowIfDisposed();

        var dirtyEntities = GetDirtyEntities().ToList();

        foreach (var entity in dirtyEntities)
        {
            MarkAsClean(entity);
        }

        _logger?.LogDebug("Accepted all changes. Marked {Count} entities as clean", dirtyEntities.Count);
    }

    /// <summary>
    /// Raises the EntityStateChanged event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnEntityStateChanged(EntityStateChangedEventArgs e)
    {
        try
        {
            EntityStateChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error occurred while raising EntityStateChanged event for entity {EntityType}",
                e.Entity.GetType().Name);
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the change tracker has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChangeTracker));
        }
    }

    /// <summary>
    /// Disposes the change tracker and clears all tracked entities.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _disposed = true;

        _logger?.LogDebug("ChangeTracker disposed");
    }
}
