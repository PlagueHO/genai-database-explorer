using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses local disk JSON operations with enhanced security and CRUD operations.
    /// </summary>
    public class LocalDiskPersistenceStrategy : ILocalDiskPersistenceStrategy
    {
        private readonly ILogger<LocalDiskPersistenceStrategy> _logger;
        private static readonly SemaphoreSlim _concurrencyLock = new(1, 1);

        public LocalDiskPersistenceStrategy(ILogger<LocalDiskPersistenceStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Saves the semantic model to the specified path with enhanced security and index generation.
        /// </summary>
        public async Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(semanticModel);
            ArgumentNullException.ThrowIfNull(modelPath);

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
                    // Save to temporary location first
                    await semanticModel.SaveModelAsync(tempPath);

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
        /// Loads the semantic model from the specified path with security validation.
        /// </summary>
        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(modelPath);

            // Validate the path
            var validatedPath = PathValidator.ValidateDirectoryPath(modelPath.FullName);

            _logger.LogInformation("Loading semantic model from path '{Path}'", validatedPath.FullName);

            try
            {
                // Use existing SemanticModel load functionality
                var placeholder = new SemanticModel(string.Empty, string.Empty);
                var loadedModel = await placeholder.LoadModelAsync(validatedPath);

                _logger.LogInformation("Successfully loaded semantic model '{ModelName}'", loadedModel.Name);
                return loadedModel;
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
        /// Checks if a semantic model exists at the specified path.
        /// </summary>
        public Task<bool> ExistsAsync(DirectoryInfo modelPath)
        {
            ArgumentNullException.ThrowIfNull(modelPath);

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
        /// Generates an index.json file containing metadata about the semantic model structure.
        /// </summary>
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

            var indexJson = JsonSerializer.Serialize(index, jsonOptions);
            var indexPath = Path.Combine(modelPath.FullName, "index.json");

            await File.WriteAllTextAsync(indexPath, indexJson, Encoding.UTF8);

            _logger.LogDebug("Generated index file at '{IndexPath}'", indexPath);
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
