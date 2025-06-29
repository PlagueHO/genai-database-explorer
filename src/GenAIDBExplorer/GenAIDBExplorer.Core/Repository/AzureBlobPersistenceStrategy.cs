using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Configuration;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses Azure Blob Storage with hierarchical blob naming.
    /// Implements security best practices with DefaultAzureCredential and managed identities.
    /// </summary>
    /// <remarks>
    /// This implementation follows Azure best practices:
    /// - Uses DefaultAzureCredential for authentication (supports managed identities)
    /// - Implements hierarchical blob naming for efficient organization
    /// - Includes proper error handling with exponential backoff retry
    /// - Supports concurrent operations with proper throttling
    /// - Maintains index blob for fast metadata access
    /// - Uses secure connection with encryption in transit
    /// 
    /// Blob structure:
    /// - models/{modelName}/index.json (model metadata and entity index)
    /// - models/{modelName}/semanticmodel.json (main model document)
    /// - models/{modelName}/tables/{tableName}.json (individual table documents)
    /// - models/{modelName}/views/{viewName}.json (individual view documents)
    /// - models/{modelName}/storedprocedures/{procedureName}.json (individual stored procedure documents)
    /// 
    /// References:
    /// - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
    /// - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started
    /// </remarks>
    public class AzureBlobPersistenceStrategy : IAzureBlobPersistenceStrategy
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly AzureBlobStorageConfiguration _configuration;
        private readonly ILogger<AzureBlobPersistenceStrategy> _logger;
        private readonly SemaphoreSlim _concurrencySemaphore;

        /// <summary>
        /// Initializes a new instance of the AzureBlobPersistenceStrategy class.
        /// </summary>
        /// <param name="configuration">Azure Blob Storage configuration options.</param>
        /// <param name="logger">Logger for structured logging.</param>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public AzureBlobPersistenceStrategy(
            IOptions<AzureBlobStorageConfiguration> configuration,
            ILogger<AzureBlobPersistenceStrategy> logger)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_configuration.AccountEndpoint))
            {
                throw new ArgumentException("AccountEndpoint is required in Azure Blob Storage configuration.", nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(_configuration.ContainerName))
            {
                throw new ArgumentException("ContainerName is required in Azure Blob Storage configuration.", nameof(configuration));
            }

            // Initialize concurrency control
            _concurrencySemaphore = new SemaphoreSlim(_configuration.MaxConcurrentOperations, _configuration.MaxConcurrentOperations);

            try
            {
                // Create BlobServiceClient using DefaultAzureCredential for secure authentication
                // This supports managed identities, service principals, and developer credentials
                var credential = new DefaultAzureCredential();
                var clientOptions = new BlobClientOptions
                {
                    Retry =
                    {
                        MaxRetries = 3,
                        Mode = RetryMode.Exponential,
                        Delay = TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(30)
                    }
                };

                _blobServiceClient = new BlobServiceClient(new Uri(_configuration.AccountEndpoint), credential, clientOptions);
                _containerClient = _blobServiceClient.GetBlobContainerClient(_configuration.ContainerName);

                _logger.LogInformation("Initialized Azure Blob Storage persistence strategy with endpoint {AccountEndpoint} and container {ContainerName}",
                    _configuration.AccountEndpoint, _configuration.ContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Blob Storage client with endpoint {AccountEndpoint}",
                    _configuration.AccountEndpoint);
                throw;
            }
        }

        /// <summary>
        /// Saves the semantic model to Azure Blob Storage using hierarchical blob naming.
        /// </summary>
        /// <param name="semanticModel">The semantic model to save.</param>
        /// <param name="modelPath">The logical path (model name) - used as blob prefix.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when save operation fails.</exception>
        public async Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            var modelName = PathValidator.ValidateAndSanitizePath(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting save operation for semantic model {ModelName} to Azure Blob Storage", modelName);

            try
            {
                // Ensure container exists
                await EnsureContainerExistsAsync();

                var blobPrefix = GetBlobPrefix(modelName);
                var concurrentTasks = new List<Task>();

                // Save main semantic model document
                var mainModelTask = SaveBlobAsync($"{blobPrefix}/semanticmodel.json", semanticModel, useEntityConverters: true);
                concurrentTasks.Add(mainModelTask);

                // Save tables concurrently
                foreach (var table in semanticModel.Tables)
                {
                    var tableName = EntityNameSanitizer.SanitizeEntityName(table.Name);
                    var tableTask = SaveBlobAsync($"{blobPrefix}/tables/{tableName}.json", table, useEntityConverters: false);
                    concurrentTasks.Add(tableTask);
                }

                // Save views concurrently
                foreach (var view in semanticModel.Views)
                {
                    var viewName = EntityNameSanitizer.SanitizeEntityName(view.Name);
                    var viewTask = SaveBlobAsync($"{blobPrefix}/views/{viewName}.json", view, useEntityConverters: false);
                    concurrentTasks.Add(viewTask);
                }

                // Save stored procedures concurrently
                foreach (var storedProcedure in semanticModel.StoredProcedures)
                {
                    var procedureName = EntityNameSanitizer.SanitizeEntityName(storedProcedure.Name);
                    var procedureTask = SaveBlobAsync($"{blobPrefix}/storedprocedures/{procedureName}.json", storedProcedure, useEntityConverters: false);
                    concurrentTasks.Add(procedureTask);
                }

                // Wait for all saves to complete
                await Task.WhenAll(concurrentTasks);

                // Create index blob with model metadata and entity listing
                await CreateIndexBlobAsync(blobPrefix, semanticModel);

                stopwatch.Stop();
                _logger.LogInformation("Successfully saved semantic model {ModelName} to Azure Blob Storage in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError(ex, "Azure Storage container {ContainerName} not found for model {ModelName}",
                    _configuration.ContainerName, modelName);
                throw new InvalidOperationException($"Storage container '{_configuration.ContainerName}' not found. Ensure the container exists and you have proper permissions.", ex);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied to Azure Storage container {ContainerName} for model {ModelName}",
                    _configuration.ContainerName, modelName);
                throw new InvalidOperationException($"Access denied to storage container '{_configuration.ContainerName}'. Ensure your managed identity has the 'Storage Blob Data Contributor' role.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save semantic model {ModelName} to Azure Blob Storage after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to save semantic model '{modelName}' to Azure Blob Storage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads the semantic model from Azure Blob Storage.
        /// </summary>
        /// <param name="modelPath">The logical path (model name) - used as blob prefix.</param>
        /// <returns>The loaded semantic model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when modelPath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the semantic model is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when load operation fails.</exception>
        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            var modelName = PathValidator.ValidateAndSanitizePath(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting load operation for semantic model {ModelName} from Azure Blob Storage", modelName);

            try
            {
                var blobPrefix = GetBlobPrefix(modelName);
                var mainModelBlobName = $"{blobPrefix}/semanticmodel.json";

                // Load main semantic model document
                var mainModelBlob = _containerClient.GetBlobClient(mainModelBlobName);

                if (!await mainModelBlob.ExistsAsync())
                {
                    throw new FileNotFoundException($"Semantic model '{modelName}' not found in Azure Blob Storage.", mainModelBlobName);
                }

                // Download and deserialize main model
                var response = await mainModelBlob.DownloadContentAsync();
                var jsonContent = response.Value.Content.ToString();
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

                var semanticModel = JsonSerializer.Deserialize<SemanticModel>(jsonContent, jsonOptions)
                    ?? throw new InvalidOperationException($"Failed to deserialize semantic model '{modelName}'.");

                // Load entities concurrently
                var loadTasks = new List<Task>();

                // Load tables
                foreach (var table in semanticModel.Tables)
                {
                    var tableName = EntityNameSanitizer.SanitizeEntityName(table.Name);
                    var loadTask = LoadEntityAsync($"{blobPrefix}/tables/{tableName}.json", async jsonContent =>
                    {
                        // For blob storage, we need to create a temporary directory approach
                        // This is a workaround since the entities expect DirectoryInfo
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);
                        try
                        {
                            var filePath = Path.Combine(tempDir, $"{tableName}.json");
                            await File.WriteAllTextAsync(filePath, jsonContent);
                            await table.LoadModelAsync(new DirectoryInfo(tempDir));
                        }
                        finally
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                        }
                    });
                    loadTasks.Add(loadTask);
                }

                // Load views
                foreach (var view in semanticModel.Views)
                {
                    var viewName = EntityNameSanitizer.SanitizeEntityName(view.Name);
                    var loadTask = LoadEntityAsync($"{blobPrefix}/views/{viewName}.json", async jsonContent =>
                    {
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);
                        try
                        {
                            var filePath = Path.Combine(tempDir, $"{viewName}.json");
                            await File.WriteAllTextAsync(filePath, jsonContent);
                            await view.LoadModelAsync(new DirectoryInfo(tempDir));
                        }
                        finally
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                        }
                    });
                    loadTasks.Add(loadTask);
                }

                // Load stored procedures
                foreach (var storedProcedure in semanticModel.StoredProcedures)
                {
                    var procedureName = EntityNameSanitizer.SanitizeEntityName(storedProcedure.Name);
                    var loadTask = LoadEntityAsync($"{blobPrefix}/storedprocedures/{procedureName}.json", async jsonContent =>
                    {
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);
                        try
                        {
                            var filePath = Path.Combine(tempDir, $"{procedureName}.json");
                            await File.WriteAllTextAsync(filePath, jsonContent);
                            await storedProcedure.LoadModelAsync(new DirectoryInfo(tempDir));
                        }
                        finally
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                        }
                    });
                    loadTasks.Add(loadTask);
                }

                // Wait for all loads to complete
                await Task.WhenAll(loadTasks);

                stopwatch.Stop();
                _logger.LogInformation("Successfully loaded semantic model {ModelName} from Azure Blob Storage in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);

                return semanticModel;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Semantic model {ModelName} not found in Azure Blob Storage", modelName);
                throw new FileNotFoundException($"Semantic model '{modelName}' not found in Azure Blob Storage.", ex);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied to Azure Storage container {ContainerName} for model {ModelName}",
                    _configuration.ContainerName, modelName);
                throw new InvalidOperationException($"Access denied to storage container '{_configuration.ContainerName}'. Ensure your managed identity has the 'Storage Blob Data Reader' role.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load semantic model {ModelName} from Azure Blob Storage after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to load semantic model '{modelName}' from Azure Blob Storage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a semantic model exists in Azure Blob Storage.
        /// </summary>
        /// <param name="modelPath">The logical path (model name) to check.</param>
        /// <returns>True if the model exists; otherwise, false.</returns>
        public async Task<bool> ExistsAsync(DirectoryInfo modelPath)
        {
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            var modelName = PathValidator.ValidateAndSanitizePath(modelPath.Name);

            try
            {
                var blobPrefix = GetBlobPrefix(modelName);
                var mainModelBlobName = $"{blobPrefix}/semanticmodel.json";
                var mainModelBlob = _containerClient.GetBlobClient(mainModelBlobName);

                var exists = await mainModelBlob.ExistsAsync();

                _logger.LogDebug("Semantic model {ModelName} exists check: {Exists}", modelName, exists.Value);
                return exists.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied when checking existence of model {ModelName}", modelName);
                throw new InvalidOperationException($"Access denied when checking model existence. Ensure your managed identity has proper permissions.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence of semantic model {ModelName}", modelName);
                return false;
            }
        }

        /// <summary>
        /// Lists all available semantic models in the Azure Blob Storage container.
        /// </summary>
        /// <param name="rootPath">The root path (ignored for blob storage - uses container scope).</param>
        /// <returns>An enumerable of model names found in the container.</returns>
        public async Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath)
        {
            try
            {
                var modelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var prefix = string.IsNullOrWhiteSpace(_configuration.BlobPrefix)
                    ? "models/"
                    : $"{_configuration.BlobPrefix.TrimEnd('/')}/models/";

                await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
                {
                    // Extract model name from blob path: models/{modelName}/...
                    var relativePath = blobItem.Name.Substring(prefix.Length);
                    var firstSlashIndex = relativePath.IndexOf('/');

                    if (firstSlashIndex > 0)
                    {
                        var modelName = relativePath.Substring(0, firstSlashIndex);
                        modelNames.Add(modelName);
                    }
                }

                var result = modelNames.ToList();
                _logger.LogInformation("Found {Count} semantic models in Azure Blob Storage container {ContainerName}",
                    result.Count, _configuration.ContainerName);

                return result;
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied when listing models in container {ContainerName}", _configuration.ContainerName);
                throw new InvalidOperationException($"Access denied when listing models. Ensure your managed identity has the 'Storage Blob Data Reader' role.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list semantic models in Azure Blob Storage container {ContainerName}",
                    _configuration.ContainerName);
                throw new InvalidOperationException($"Failed to list semantic models in Azure Blob Storage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a semantic model from Azure Blob Storage.
        /// </summary>
        /// <param name="modelPath">The logical path (model name) to delete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        public async Task DeleteModelAsync(DirectoryInfo modelPath)
        {
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            var modelName = PathValidator.ValidateAndSanitizePath(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting delete operation for semantic model {ModelName} from Azure Blob Storage", modelName);

            try
            {
                var blobPrefix = GetBlobPrefix(modelName);
                var deleteTasks = new List<Task>();

                // List all blobs with the model prefix
                await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: $"{blobPrefix}/"))
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    var deleteTask = DeleteBlobAsync(blobClient, blobItem.Name);
                    deleteTasks.Add(deleteTask);
                }

                // Wait for all deletions to complete
                await Task.WhenAll(deleteTasks);

                stopwatch.Stop();
                _logger.LogInformation("Successfully deleted semantic model {ModelName} from Azure Blob Storage in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied when deleting model {ModelName}", modelName);
                throw new InvalidOperationException($"Access denied when deleting model. Ensure your managed identity has the 'Storage Blob Data Contributor' role.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete semantic model {ModelName} from Azure Blob Storage after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to delete semantic model '{modelName}' from Azure Blob Storage: {ex.Message}", ex);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Ensures the Azure Blob Storage container exists and creates it if necessary.
        /// </summary>
        private async Task EnsureContainerExistsAsync()
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync();
                _logger.LogDebug("Ensured container {ContainerName} exists", _configuration.ContainerName);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError(ex, "Access denied when creating/accessing container {ContainerName}", _configuration.ContainerName);
                throw new InvalidOperationException($"Access denied to container '{_configuration.ContainerName}'. Ensure your managed identity has proper permissions.", ex);
            }
        }

        /// <summary>
        /// Gets the blob prefix for a model, including any configured prefix.
        /// </summary>
        private string GetBlobPrefix(string modelName)
        {
            var sanitizedModelName = EntityNameSanitizer.SanitizeEntityName(modelName);

            return string.IsNullOrWhiteSpace(_configuration.BlobPrefix)
                ? $"models/{sanitizedModelName}"
                : $"{_configuration.BlobPrefix.TrimEnd('/')}/models/{sanitizedModelName}";
        }

        /// <summary>
        /// Saves a single object as a JSON blob with concurrency control.
        /// </summary>
        private async Task SaveBlobAsync<T>(string blobName, T data, bool useEntityConverters)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

                // Add entity converters for main model to avoid circular references
                if (useEntityConverters)
                {
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelTableJsonConverter());
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelViewJsonConverter());
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelStoredProcedureJsonConverter());
                }

                var jsonContent = JsonSerializer.Serialize(data, jsonOptions);
                var content = BinaryData.FromString(jsonContent);

                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(content, overwrite: true);

                _logger.LogDebug("Saved blob {BlobName} ({Size} bytes)", blobName, content.ToArray().Length);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Loads an entity from a blob asynchronously.
        /// </summary>
        private async Task LoadEntityAsync(string blobName, Func<string, Task> loadFromJsonFunc)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    var response = await blobClient.DownloadContentAsync();
                    var jsonContent = response.Value.Content.ToString();
                    await loadFromJsonFunc(jsonContent);

                    _logger.LogDebug("Loaded entity from blob {BlobName}", blobName);
                }
                else
                {
                    _logger.LogWarning("Entity blob {BlobName} not found", blobName);
                }
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Creates an index blob with model metadata and entity listing for fast discovery.
        /// </summary>
        private async Task CreateIndexBlobAsync(string blobPrefix, SemanticModel semanticModel)
        {
            var index = new
            {
                ModelName = semanticModel.Name,
                Source = semanticModel.Source,
                Description = semanticModel.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                TablesCount = semanticModel.Tables.Count,
                ViewsCount = semanticModel.Views.Count,
                StoredProceduresCount = semanticModel.StoredProcedures.Count,
                Tables = semanticModel.Tables.Select(t => new { t.Name, t.Schema, RelativePath = $"tables/{EntityNameSanitizer.SanitizeEntityName(t.Name)}.json" }),
                Views = semanticModel.Views.Select(v => new { v.Name, v.Schema, RelativePath = $"views/{EntityNameSanitizer.SanitizeEntityName(v.Name)}.json" }),
                StoredProcedures = semanticModel.StoredProcedures.Select(sp => new { sp.Name, sp.Schema, RelativePath = $"storedprocedures/{EntityNameSanitizer.SanitizeEntityName(sp.Name)}.json" })
            };

            await SaveBlobAsync($"{blobPrefix}/index.json", index, useEntityConverters: false);
        }

        /// <summary>
        /// Deletes a single blob with proper error handling.
        /// </summary>
        private async Task DeleteBlobAsync(BlobClient blobClient, string blobName)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                await blobClient.DeleteIfExistsAsync();
                _logger.LogDebug("Deleted blob {BlobName}", blobName);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the Azure Blob persistence strategy.
        /// </summary>
        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
        }

        #endregion
    }
}
