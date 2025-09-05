using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
using GenAIDBExplorer.Core.Repository.Helpers;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses local disk JSON operations with enhanced security and CRUD operations.
    /// </summary>
    public class LocalDiskPersistenceStrategy : ILocalDiskPersistenceStrategy
    {
        private readonly ILogger<LocalDiskPersistenceStrategy> _logger;
        private readonly ISecureJsonSerializer _secureJsonSerializer;
        private static readonly SemaphoreSlim _concurrencyLock = new(1, 1);

        public LocalDiskPersistenceStrategy(ILogger<LocalDiskPersistenceStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Back-compat ctor: instantiate a default secure serializer
            _secureJsonSerializer = new SecureJsonSerializer(new NullLogger<SecureJsonSerializer>());
        }

        /// <summary>
        /// Preferred constructor with secure JSON serializer injection.
        /// </summary>
        public LocalDiskPersistenceStrategy(
            ILogger<LocalDiskPersistenceStrategy> logger,
            ISecureJsonSerializer secureJsonSerializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secureJsonSerializer = secureJsonSerializer ?? throw new ArgumentNullException(nameof(secureJsonSerializer));
        }

        /// <summary>
        /// Saves the semantic model to the specified path with enhanced security and index generation.
        /// </summary>
        public async Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(semanticModel);
            ArgumentNullException.ThrowIfNull(modelPath);

            // Enhanced input validation
            ValidateInputSecurity(semanticModel, modelPath);

            // Validate and sanitize the path
            var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);

            _logger.LogInformation("Saving semantic model '{ModelName}' to path '{Path}'",
                semanticModel.Name, validatedPath.FullName);

            await _concurrencyLock.WaitAsync();
            try
            {
                using var tempDirectoryManager = new TempDirectoryManager(_logger);
                tempDirectoryManager.EnsureExists();

                try
                {
                    // Save to temporary location first using domain serializer for main model and entities
                    await SaveSemanticModelAndEntitiesAsync(semanticModel, tempDirectoryManager.Path);

                    // Ensure target directory exists
                    Directory.CreateDirectory(validatedPath.FullName);

                    // Atomic move from temp to final location
                    await tempDirectoryManager.MoveContentsToAsync(validatedPath);

                    _logger.LogInformation("Successfully saved semantic model '{ModelName}'", semanticModel.Name);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogError(ex, "Failed to save semantic model '{ModelName}' to '{Path}'",
                        semanticModel.Name, validatedPath.FullName);
                    throw new InvalidOperationException($"Failed to save semantic model: {ex.Message}", ex);
                }
            }
            finally
            {
                _concurrencyLock.Release();
            }
        }

        /// <summary>
        /// Saves the semantic model and entity files using the secure serializer.
        /// </summary>
        private async Task SaveSemanticModelAndEntitiesAsync(SemanticModel semanticModel, DirectoryInfo tempPath)
        {
            // Save to temporary location first using domain serializer for main model and entities
            await semanticModel.SaveModelAsync(tempPath);

            // Phase 2/3 security: re-write all JSON (semanticmodel + entities) via secure serializer.
            // - Preserves existing structure when no embeddings are present.
            // - Prepares for envelope format { data, embedding } when embeddings are available.
            await RewriteFilesWithSecureSerializerAsync(semanticModel, tempPath);
        }

        /// <summary>
        /// Re-writes the semantic model and entity files in the given folder using the secure JSON serializer,
        /// preserving current format (no envelope) when no embeddings are provided.
        /// </summary>
        private async Task RewriteFilesWithSecureSerializerAsync(SemanticModel semanticModel, DirectoryInfo tempPath)
        {
            _logger.LogDebug("RewriteFilesWithSecureSerializerAsync started for model: {ModelName}", semanticModel.Name);

            // Materialize lazy-loaded data into backing fields before serialization
            await MaterializeLazyLoadedDataAsync(semanticModel, tempPath);

            _logger.LogDebug("MaterializeLazyLoadedDataAsync completed for model: {ModelName}", semanticModel.Name);

            // Re-write semantic model using helper
            var semanticModelFileManager = new SemanticModelFileManager(_secureJsonSerializer, _logger);
            await semanticModelFileManager.SaveSemanticModelAsync(semanticModel, tempPath);

            // Re-write entities using helper
            var entityFileManager = new EntityFileManager(_secureJsonSerializer, _logger);

            var tables = await semanticModel.GetTablesAsync();
            await entityFileManager.SaveEntitiesAsync(tables, tempPath, LocalDiskPersistenceConstants.Folders.Tables,
                table => $"{table.Schema}.{table.Name}.json");

            var views = await semanticModel.GetViewsAsync();
            await entityFileManager.SaveEntitiesAsync(views, tempPath, LocalDiskPersistenceConstants.Folders.Views,
                view => $"{view.Schema}.{view.Name}.json");

            var storedProcedures = await semanticModel.GetStoredProceduresAsync();
            await entityFileManager.SaveEntitiesAsync(storedProcedures, tempPath, LocalDiskPersistenceConstants.Folders.StoredProcedures,
                sp => $"{sp.Schema}.{sp.Name}.json");
        }

        /// <summary>
        /// Materializes lazy-loaded data into backing fields for JSON serialization.
        /// Loads data directly from the temporary directory since lazy proxies are configured for the original model path.
        /// </summary>
        private async Task MaterializeLazyLoadedDataAsync(SemanticModel semanticModel, DirectoryInfo tempPath)
        {
            _logger.LogDebug("Starting materialization of lazy-loaded data by accessing collections to trigger lazy loading");

            // Materialize lazy-loaded data by accessing the collections, which will trigger the lazy loading proxies
            // to load from the actual semantic model directory where the files exist
            var tables = await semanticModel.GetTablesAsync();
            var views = await semanticModel.GetViewsAsync();
            var storedProcedures = await semanticModel.GetStoredProceduresAsync();

            var tableCount = tables?.Count() ?? 0;
            var viewCount = views?.Count() ?? 0;
            var procedureCount = storedProcedures?.Count() ?? 0;

            _logger.LogDebug("Materialized {TableCount} tables, {ViewCount} views, {ProcedureCount} procedures via lazy loading",
                tableCount, viewCount, procedureCount);

            // The lazy loading has now populated the backing collections, so no need to manually clear/add
            _logger.LogDebug("Lazy loading completed - backing collections should now be populated: {BackingTableCount} tables, {BackingViewCount} views, {BackingProcedureCount} procedures",
                semanticModel.Tables.Count, semanticModel.Views.Count, semanticModel.StoredProcedures.Count);
        }

        /// <summary>
        /// Loads the semantic model from the specified path with security validation.
        /// </summary>
        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(modelPath);

            // Enhanced input validation
            EntityNameSanitizer.ValidateInputSecurity(modelPath.FullName, nameof(modelPath));

            // Validate the path
            var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);

            _logger.LogInformation("Loading semantic model from path '{Path}'", validatedPath.FullName);

            try
            {
                // Load main semantic model using helper
                var semanticModelFileManager = new SemanticModelFileManager(_secureJsonSerializer, _logger);
                var semanticModel = await semanticModelFileManager.LoadSemanticModelAsync(validatedPath);

                // Load entities using helper
                var entityFileManager = new EntityFileManager(_secureJsonSerializer, _logger);
                var loadTasks = new List<Task>();

                loadTasks.AddRange(await CreateEntityLoadTasks(semanticModel.Tables, validatedPath, LocalDiskPersistenceConstants.Folders.Tables, entityFileManager));
                loadTasks.AddRange(await CreateEntityLoadTasks(semanticModel.Views, validatedPath, LocalDiskPersistenceConstants.Folders.Views, entityFileManager));
                loadTasks.AddRange(await CreateEntityLoadTasks(semanticModel.StoredProcedures, validatedPath, LocalDiskPersistenceConstants.Folders.StoredProcedures, entityFileManager));

                await Task.WhenAll(loadTasks);

                _logger.LogInformation("Successfully loaded semantic model '{ModelName}'", semanticModel.Name);
                return semanticModel;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                _logger.LogError(ex, "Semantic model not found at path '{Path}'", validatedPath.FullName);
                throw;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                _logger.LogError(ex, "Failed to load semantic model from path '{Path}'", validatedPath.FullName);
                throw new InvalidOperationException($"Failed to load semantic model: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates loading tasks for a collection of entities.
        /// </summary>
        private async Task<IEnumerable<Task>> CreateEntityLoadTasks<T>(IEnumerable<T> entities, DirectoryInfo modelPath, string folderName, EntityFileManager entityFileManager)
            where T : class
        {
            var tasks = new List<Task>();
            var folderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, folderName));

            if (!Directory.Exists(folderPath.FullName))
                return tasks;

            foreach (var entity in entities)
            {
                if (entity is Models.SemanticModel.SemanticModelTable table)
                {
                    tasks.Add(LoadTableAsync(table, folderPath, entityFileManager));
                }
                else if (entity is Models.SemanticModel.SemanticModelView view)
                {
                    tasks.Add(LoadViewAsync(view, folderPath, entityFileManager));
                }
                else if (entity is Models.SemanticModel.SemanticModelStoredProcedure sp)
                {
                    tasks.Add(LoadStoredProcedureAsync(sp, folderPath, entityFileManager));
                }
            }

            return await Task.FromResult(tasks);
        }

        private static async Task LoadTableAsync(Models.SemanticModel.SemanticModelTable table, DirectoryInfo folderPath, EntityFileManager entityFileManager)
        {
            var fileName = $"{table.Schema}.{table.Name}.json";
            var filePath = Path.Combine(folderPath.FullName, fileName);
            await LoadEntityFileAsync(filePath, async rawJson =>
            {
                using var tempDirectoryManager = new TempDirectoryManager(NullLogger.Instance);
                tempDirectoryManager.EnsureExists();

                var tempFile = Path.Combine(tempDirectoryManager.Path.FullName, fileName);
                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                await table.LoadModelAsync(tempDirectoryManager.Path);
            });
        }

        private static async Task LoadViewAsync(Models.SemanticModel.SemanticModelView view, DirectoryInfo folderPath, EntityFileManager entityFileManager)
        {
            var fileName = $"{view.Schema}.{view.Name}.json";
            var filePath = Path.Combine(folderPath.FullName, fileName);
            await LoadEntityFileAsync(filePath, async rawJson =>
            {
                using var tempDirectoryManager = new TempDirectoryManager(NullLogger.Instance);
                tempDirectoryManager.EnsureExists();

                var tempFile = Path.Combine(tempDirectoryManager.Path.FullName, fileName);
                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                await view.LoadModelAsync(tempDirectoryManager.Path);
            });
        }

        private static async Task LoadStoredProcedureAsync(Models.SemanticModel.SemanticModelStoredProcedure sp, DirectoryInfo folderPath, EntityFileManager entityFileManager)
        {
            var fileName = $"{sp.Schema}.{sp.Name}.json";
            var filePath = Path.Combine(folderPath.FullName, fileName);
            await LoadEntityFileAsync(filePath, async rawJson =>
            {
                using var tempDirectoryManager = new TempDirectoryManager(NullLogger.Instance);
                tempDirectoryManager.EnsureExists();

                var tempFile = Path.Combine(tempDirectoryManager.Path.FullName, fileName);
                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                await sp.LoadModelAsync(tempDirectoryManager.Path);
            });
        }

        private static async Task LoadEntityFileAsync(string filePath, Func<string, Task> processJson)
        {
            if (!File.Exists(filePath))
            {
                // Missing entity file is non-fatal; skip
                return;
            }
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            await processJson(json);
        }

        /// <summary>
        /// Loads the raw JSON content for a single entity file. Unwraps envelope { data, embedding } when present.
        /// </summary>
        public Task<string?> LoadEntityContentAsync(DirectoryInfo modelPath, string relativeEntityPath, CancellationToken cancellationToken)
        {
            if (modelPath == null) throw new ArgumentNullException(nameof(modelPath));
            if (string.IsNullOrWhiteSpace(relativeEntityPath)) throw new ArgumentNullException(nameof(relativeEntityPath));

            var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);
            var filePath = Path.Combine(validatedPath.FullName, relativeEntityPath);

            if (!File.Exists(filePath)) return Task.FromResult<string?>(null);

            // Read synchronously then return as completed task to keep API simple
            var json = File.ReadAllText(filePath, Encoding.UTF8);

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                        {
                            return Task.FromResult<string?>(prop.Value.GetRawText());
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors
            }

            return Task.FromResult<string?>(json);
        }

        private static string ExtractEnvelopeDataOrPassThrough(string jsonContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Case-insensitive lookup for "data" property to support varied serializers
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                        {
                            return prop.Value.GetRawText();
                        }
                    }
                }
            }
            catch
            {
                // Fallback to pass-through on any parse error
            }
            return jsonContent;
        }

        /// <summary>
        /// Checks if a semantic model exists at the specified path.
        /// </summary>
        public Task<bool> ExistsAsync(DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(modelPath);

            // Enhanced input validation
            EntityNameSanitizer.ValidateInputSecurity(modelPath.FullName, nameof(modelPath));

            try
            {
                var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);

                // Check if directory exists and contains the semantic model file
                if (!validatedPath.Exists)
                    return Task.FromResult(false);

                var semanticModelFile = Path.Combine(validatedPath.FullName, "semanticmodel.json");
                var exists = File.Exists(semanticModelFile);

                _logger.LogDebug("Model existence check for path '{Path}': {Exists}", validatedPath.FullName, exists);
                return Task.FromResult(exists);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Failed to check model existence at path '{Path}'", modelPath.FullName);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Lists all available semantic models in the specified root directory.
        /// </summary>
        public async Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath)
        {
            ArgumentNullException.ThrowIfNull(rootPath);

            // Enhanced input validation
            EntityNameSanitizer.ValidateInputSecurity(rootPath.FullName, nameof(rootPath));

            try
            {
                var validatedPath = PathValidator.ValidateDirectoryPath(rootPath.FullName);

                if (!validatedPath.Exists)
                {
                    _logger.LogInformation("Root path '{Path}' does not exist, returning empty list", validatedPath.FullName);
                    return Enumerable.Empty<string>();
                }

                var modelDirectories = new List<string>();

                await Task.Run(() =>
                {
                    foreach (var directory in validatedPath.EnumerateDirectories())
                    {
                        var semanticModelFile = Path.Combine(directory.FullName, "semanticmodel.json");
                        if (File.Exists(semanticModelFile))
                        {
                            modelDirectories.Add(directory.Name);
                        }
                    }
                });

                _logger.LogInformation("Found {Count} semantic models in root path '{Path}'",
                    modelDirectories.Count, validatedPath.FullName);

                return modelDirectories;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is UnauthorizedAccessException || ex is IOException)
            {
                _logger.LogError(ex, "Failed to list models in root path '{Path}'", rootPath.FullName);
                throw new InvalidOperationException($"Failed to list models: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a semantic model from the specified path with safety checks.
        /// </summary>
        public async Task DeleteModelAsync(DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(modelPath);

            // Enhanced input validation
            EntityNameSanitizer.ValidateInputSecurity(modelPath.FullName, nameof(modelPath));

            var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);

            _logger.LogInformation("Deleting semantic model at path '{Path}'", validatedPath.FullName);

            await _concurrencyLock.WaitAsync();
            try
            {
                if (!validatedPath.Exists)
                {
                    _logger.LogWarning("Cannot delete non-existent model at path '{Path}'", validatedPath.FullName);
                    return;
                }

                // Verify this is actually a semantic model directory
                var semanticModelFile = Path.Combine(validatedPath.FullName, "semanticmodel.json");
                if (!File.Exists(semanticModelFile))
                {
                    throw new InvalidOperationException($"Directory does not contain a semantic model: {validatedPath.FullName}");
                }

                // Acquire exclusive lock on the directory by trying to create a lock file
                var lockFilePath = Path.Combine(validatedPath.FullName, ".delete_lock");
                FileStream? lockFile = null;
                try
                {
                    lockFile = File.Create(lockFilePath, 1, FileOptions.None);

                    // Delete the directory recursively
                    await Task.Run(() =>
                    {
                        lockFile.Dispose(); // Ensure lock file is closed before deleting directory
                        lockFile = null;
                        validatedPath.Delete(true);
                    });
                }
                finally
                {
                    lockFile?.Dispose();
                    // Clean up lock file if it still exists
                    if (File.Exists(lockFilePath))
                    {
                        try { File.Delete(lockFilePath); } catch { /* Best effort cleanup */ }
                    }
                }

                _logger.LogInformation("Successfully deleted semantic model at path '{Path}'", validatedPath.FullName);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Failed to delete semantic model at path '{Path}'", validatedPath.FullName);
                throw new InvalidOperationException($"Failed to delete semantic model: {ex.Message}", ex);
            }
            finally
            {
                _concurrencyLock.Release();
            }
        }

        /// <summary>
        /// Validates input security for semantic model operations.
        /// </summary>
        /// <param name="semanticModel">The semantic model to validate.</param>
        /// <param name="modelPath">The model path to validate.</param>
        private static void ValidateInputSecurity(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            // Validate semantic model properties
            if (!string.IsNullOrWhiteSpace(semanticModel.Name))
            {
                EntityNameSanitizer.ValidateInputSecurity(semanticModel.Name, nameof(semanticModel.Name));
            }

            if (!string.IsNullOrWhiteSpace(semanticModel.Description))
            {
                EntityNameSanitizer.ValidateInputSecurity(semanticModel.Description, nameof(semanticModel.Description));
            }

            // Validate path security
            EntityNameSanitizer.ValidateInputSecurity(modelPath.FullName, nameof(modelPath));

            // Validate entity names in collections
            foreach (var table in semanticModel.Tables)
            {
                if (!string.IsNullOrWhiteSpace(table.Name))
                {
                    EntityNameSanitizer.ValidateInputSecurity(table.Name, $"Table.Name");
                }
                if (!string.IsNullOrWhiteSpace(table.Schema))
                {
                    EntityNameSanitizer.ValidateInputSecurity(table.Schema, $"Table.Schema");
                }
            }

            foreach (var view in semanticModel.Views)
            {
                if (!string.IsNullOrWhiteSpace(view.Name))
                {
                    EntityNameSanitizer.ValidateInputSecurity(view.Name, $"View.Name");
                }
                if (!string.IsNullOrWhiteSpace(view.Schema))
                {
                    EntityNameSanitizer.ValidateInputSecurity(view.Schema, $"View.Schema");
                }
            }

            foreach (var procedure in semanticModel.StoredProcedures)
            {
                if (!string.IsNullOrWhiteSpace(procedure.Name))
                {
                    EntityNameSanitizer.ValidateInputSecurity(procedure.Name, $"StoredProcedure.Name");
                }
                if (!string.IsNullOrWhiteSpace(procedure.Schema))
                {
                    EntityNameSanitizer.ValidateInputSecurity(procedure.Schema, $"StoredProcedure.Schema");
                }
            }
        }

        /// <summary>
        /// Moves the contents of one directory to another atomically.
        /// </summary>
        private async Task MoveDirectoryContentsAsync(DirectoryInfo source, DirectoryInfo destination)
        {
            await Task.Run(() =>
            {
                foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(source.FullName, file.FullName);
                    var destPath = Path.Combine(destination.FullName, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Move(file.FullName, destPath, true);
                }
            });
        }
    }
}
