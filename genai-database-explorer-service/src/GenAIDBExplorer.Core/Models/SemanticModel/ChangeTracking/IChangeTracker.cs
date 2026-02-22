using System;
using System.Collections.Generic;

namespace GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;

/// <summary>
/// Defines the contract for tracking changes to semantic model entities.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Marks an entity as dirty (modified).
    /// </summary>
    /// <param name="entity">The entity that has been modified.</param>
    void MarkAsDirty(object entity);

    /// <summary>
    /// Marks an entity as clean (unmodified).
    /// </summary>
    /// <param name="entity">The entity to mark as clean.</param>
    void MarkAsClean(object entity);

    /// <summary>
    /// Determines whether the specified entity has been modified.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>true if the entity is dirty (modified); otherwise, false.</returns>
    bool IsDirty(object entity);

    /// <summary>
    /// Gets all dirty (modified) entities.
    /// </summary>
    /// <returns>A collection of all dirty entities.</returns>
    IEnumerable<object> GetDirtyEntities();

    /// <summary>
    /// Gets the count of dirty (modified) entities.
    /// </summary>
    int DirtyEntityCount { get; }

    /// <summary>
    /// Determines whether there are any dirty (modified) entities.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Clears all tracked entities and resets the change tracker.
    /// </summary>
    void Clear();

    /// <summary>
    /// Accepts all changes and marks all entities as clean.
    /// </summary>
    void AcceptAllChanges();

    /// <summary>
    /// Event that is raised when an entity's dirty state changes.
    /// </summary>
    event EventHandler<EntityStateChangedEventArgs>? EntityStateChanged;
}

/// <summary>
/// Provides data for the EntityStateChanged event.
/// </summary>
public class EntityStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the EntityStateChangedEventArgs class.
    /// </summary>
    /// <param name="entity">The entity whose state changed.</param>
    /// <param name="isDirty">The new dirty state of the entity.</param>
    public EntityStateChangedEventArgs(object entity, bool isDirty)
    {
        Entity = entity;
        IsDirty = isDirty;
    }

    /// <summary>
    /// Gets the entity whose state changed.
    /// </summary>
    public object Entity { get; }

    /// <summary>
    /// Gets a value indicating whether the entity is dirty.
    /// </summary>
    public bool IsDirty { get; }
}
