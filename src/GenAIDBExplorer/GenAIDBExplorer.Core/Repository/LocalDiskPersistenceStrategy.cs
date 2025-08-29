using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
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
                // Create a temporary directory for atomic operations
                var tempPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"semanticmodel_temp_{Guid.NewGuid():N}"));

                try
                {
                    // Save to temporary location first using domain serializer for main model and entities
                    await semanticModel.SaveModelAsync(tempPath);

                    // Phase 2/3 security: re-write all JSON (semanticmodel + entities) via secure serializer.
                    // - Preserves existing structure when no embeddings are present.
                    // - Prepares for envelope format { data, embedding } when embeddings are available.
                    await RewriteFilesWithSecureSerializerAsync(semanticModel, tempPath);

                    // Generate index file
                    await GenerateIndexFileAsync(semanticModel, tempPath);

                    // Ensure target directory exists
                    Directory.CreateDirectory(validatedPath.FullName);

                    // Atomic move from temp to final location
                    await MoveDirectoryContentsAsync(tempPath, validatedPath);

                    _logger.LogInformation("Successfully saved semantic model '{ModelName}'", semanticModel.Name);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogError(ex, "Failed to save semantic model '{ModelName}' to '{Path}'",
                        semanticModel.Name, validatedPath.FullName);
                    throw new InvalidOperationException($"Failed to save semantic model: {ex.Message}", ex);
                }
                finally
                {
                    // Clean up temporary directory
                    if (tempPath.Exists)
                    {
                        try
                        {
                            tempPath.Delete(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to clean up temporary directory '{TempPath}'", tempPath.FullName);
                        }
                    }
                }
            }
            finally
            {
                _concurrencyLock.Release();
            }
        }

        /// <summary>
        /// Re-writes the semantic model and entity files in the given folder using the secure JSON serializer,
        /// preserving current format (no envelope) when no embeddings are provided.
        /// </summary>
        private async Task RewriteFilesWithSecureSerializerAsync(SemanticModel semanticModel, DirectoryInfo tempPath)
        {
            // Re-write semanticmodel.json using secure serializer and domain converters
            var semanticModelJsonPath = Path.Combine(tempPath.FullName, "semanticmodel.json");
            var modelJsonOptions = new JsonSerializerOptions { WriteIndented = true };
            modelJsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelTableJsonConverter());
            modelJsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelViewJsonConverter());
            modelJsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelStoredProcedureJsonConverter());

            var secureModelJson = await _secureJsonSerializer.SerializeAsync(semanticModel, modelJsonOptions);
            await File.WriteAllTextAsync(semanticModelJsonPath, secureModelJson, Encoding.UTF8);

            var mapper = new LocalBlobEntityMapper();
            var entityJsonOptions = new JsonSerializerOptions { WriteIndented = true };

            // Tables
            var tablesFolderPath = new DirectoryInfo(Path.Combine(tempPath.FullName, "tables"));
            if (Directory.Exists(tablesFolderPath.FullName))
            {
                foreach (var table in semanticModel.Tables)
                {
                    var filePath = Path.Combine(tablesFolderPath.FullName, $"{table.Schema}.{table.Name}.json");
                    if (File.Exists(filePath))
                    {
                        // No embeddings at this layer yet; pass null so mapper returns raw entity.
                        var persisted = mapper.ToPersistedEntity(table, null);
                        var json = await _secureJsonSerializer.SerializeAsync(persisted, entityJsonOptions);
                        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                    }
                }
            }

            // Views
            var viewsFolderPath = new DirectoryInfo(Path.Combine(tempPath.FullName, "views"));
            if (Directory.Exists(viewsFolderPath.FullName))
            {
                foreach (var view in semanticModel.Views)
                {
                    var filePath = Path.Combine(viewsFolderPath.FullName, $"{view.Schema}.{view.Name}.json");
                    if (File.Exists(filePath))
                    {
                        var persisted = mapper.ToPersistedEntity(view, null);
                        var json = await _secureJsonSerializer.SerializeAsync(persisted, entityJsonOptions);
                        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                    }
                }
            }

            // Stored Procedures
            var proceduresFolderPath = new DirectoryInfo(Path.Combine(tempPath.FullName, "storedprocedures"));
            if (Directory.Exists(proceduresFolderPath.FullName))
            {
                foreach (var sp in semanticModel.StoredProcedures)
                {
                    var filePath = Path.Combine(proceduresFolderPath.FullName, $"{sp.Schema}.{sp.Name}.json");
                    if (File.Exists(filePath))
                    {
                        var persisted = mapper.ToPersistedEntity(sp, null);
                        var json = await _secureJsonSerializer.SerializeAsync(persisted, entityJsonOptions);
                        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                    }
                }
            }
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
                // Load main semantic model (use System.Text.Json to preserve backward compatibility)
                var semanticModelFile = Path.Combine(validatedPath.FullName, "semanticmodel.json");
                if (!File.Exists(semanticModelFile))
                {
                    throw new FileNotFoundException("The semantic model file was not found.", semanticModelFile);
                }

                var json = await File.ReadAllTextAsync(semanticModelFile, Encoding.UTF8);
                // Parse and deserialize without applying input sanitizer to the entire JSON payload
                // to avoid false positives on legitimate model content.
                using (var _ = JsonDocument.Parse(json)) { }
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var semanticModel = JsonSerializer.Deserialize<SemanticModel>(json, jsonOptions)
                    ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");

                // Load entities (tables, views, stored procedures) supporting envelope { data, embedding }
                var loadTasks = new List<Task>();

                // Tables
                var tablesFolderPath = new DirectoryInfo(Path.Combine(validatedPath.FullName, "tables"));
                if (Directory.Exists(tablesFolderPath.FullName))
                {
                    foreach (var table in semanticModel.Tables)
                    {
                        var tableFile = Path.Combine(tablesFolderPath.FullName, $"{table.Schema}.{table.Name}.json");
                        loadTasks.Add(LoadEntityFileAsync(tableFile, async rawJson =>
                        {
                            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempDir);
                            try
                            {
                                var tempFile = Path.Combine(tempDir, $"{table.Schema}.{table.Name}.json");
                                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                                await table.LoadModelAsync(new DirectoryInfo(tempDir));
                            }
                            finally
                            {
                                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                            }
                        }));
                    }
                }

                // Views
                var viewsFolderPath = new DirectoryInfo(Path.Combine(validatedPath.FullName, "views"));
                if (Directory.Exists(viewsFolderPath.FullName))
                {
                    foreach (var view in semanticModel.Views)
                    {
                        var viewFile = Path.Combine(viewsFolderPath.FullName, $"{view.Schema}.{view.Name}.json");
                        loadTasks.Add(LoadEntityFileAsync(viewFile, async rawJson =>
                        {
                            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempDir);
                            try
                            {
                                var tempFile = Path.Combine(tempDir, $"{view.Schema}.{view.Name}.json");
                                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                                await view.LoadModelAsync(new DirectoryInfo(tempDir));
                            }
                            finally
                            {
                                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                            }
                        }));
                    }
                }

                // Stored Procedures
                var proceduresFolderPath = new DirectoryInfo(Path.Combine(validatedPath.FullName, "storedprocedures"));
                if (Directory.Exists(proceduresFolderPath.FullName))
                {
                    foreach (var sp in semanticModel.StoredProcedures)
                    {
                        var spFile = Path.Combine(proceduresFolderPath.FullName, $"{sp.Schema}.{sp.Name}.json");
                        loadTasks.Add(LoadEntityFileAsync(spFile, async rawJson =>
                        {
                            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempDir);
                            try
                            {
                                var tempFile = Path.Combine(tempDir, $"{sp.Schema}.{sp.Name}.json");
                                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                                await sp.LoadModelAsync(new DirectoryInfo(tempDir));
                            }
                            finally
                            {
                                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                            }
                        }));
                    }
                }

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

        private async Task GenerateIndexFileAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            var index = new
            {
                Name = semanticModel.Name,
                Source = semanticModel.Source,
                Description = semanticModel.Description,
                GeneratedAt = DateTime.UtcNow,
                Structure = new
                {
                    Tables = semanticModel.Tables.Select(t => new
                    {
                        Schema = t.Schema,
                        Name = t.Name,
                        RelativePath = Path.Combine("tables", EntityNameSanitizer.CreateSafeFileName(t.Schema, t.Name))
                    }),
                    Views = semanticModel.Views.Select(v => new
                    {
                        Schema = v.Schema,
                        Name = v.Name,
                        RelativePath = Path.Combine("views", EntityNameSanitizer.CreateSafeFileName(v.Schema, v.Name))
                    }),
                    StoredProcedures = semanticModel.StoredProcedures.Select(sp => new
                    {
                        Schema = sp.Schema,
                        Name = sp.Name,
                        RelativePath = Path.Combine("storedprocedures", EntityNameSanitizer.CreateSafeFileName(sp.Schema, sp.Name))
                    })
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var indexJson = await _secureJsonSerializer.SerializeAsync(index, jsonOptions);
            var indexPath = Path.Combine(modelPath.FullName, "index.json");

            await File.WriteAllTextAsync(indexPath, indexJson, Encoding.UTF8);

            _logger.LogDebug("Generated index file at '{IndexPath}'", indexPath);
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
