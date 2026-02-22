using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Repository abstraction for semantic model persistence.
    /// </summary>
    public interface ISemanticModelRepository
    {
        /// <summary>
        /// Saves the semantic model using the specified persistence strategy.
        /// </summary>
        Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Saves only the changes (dirty entities) in the semantic model if change tracking is enabled.
        /// Falls back to full save if change tracking is not enabled or no changes are detected.
        /// </summary>
        Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model using default options (no lazy loading, change tracking, or caching).
        /// </summary>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model using immutable options configuration.
        /// This method provides a fluent, thread-safe alternative to boolean parameter overloads.
        /// </summary>
        /// <param name="modelPath">The path to load the model from.</param>
        /// <param name="options">Immutable options configuration created via the builder pattern.</param>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, SemanticModelRepositoryOptions options);

        /// <summary>
        /// Checks if a vector exists for the specified entity with the given content hash.
        /// This enables persistence-strategy-aware vector existence checking for idempotent operations.
        /// </summary>
        /// <param name="entityType">The type of entity (e.g., "tables", "views").</param>
        /// <param name="schemaName">The schema name of the entity.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="contentHash">The content hash to check for existing vectors.</param>
        /// <param name="modelPath">The project path for LocalDisk strategy context.</param>
        /// <param name="strategyName">Optional strategy name override.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The existing content hash if found, null if not found or doesn't match.</returns>
        Task<string?> CheckVectorExistsAsync(string entityType, string schemaName, string entityName,
            string contentHash, DirectoryInfo modelPath, string? strategyName = null,
            CancellationToken cancellationToken = default);
    }
}
