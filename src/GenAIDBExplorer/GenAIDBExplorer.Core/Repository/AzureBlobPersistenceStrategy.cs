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
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;


namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses Azure Blob Storage with hierarchical blob naming.
    /// Implements security best practices with DefaultAzureCredential, managed identities,
    /// and enhanced security features including secure JSON serialization and Key Vault integration.
    /// </summary>
    /// <remarks>
    /// This implementation follows Azure best practices:
    /// - Uses DefaultAzureCredential for authentication (supports managed identities)
    /// - Implements hierarchical blob naming for efficient organization
    /// - Includes proper error handling with exponential backoff retry
    /// - Supports concurrent operations with proper throttling
    /// - Maintains index blob for fast metadata access
    /// - Uses secure connection with encryption in transit
    /// - Enhanced with secure JSON serialization to prevent injection attacks
    /// - Integrated with Azure Key Vault for secure credential management
    /// - Comprehensive security validation and audit logging
    /// 
    /// Blob structure:
    /// - models/{modelName}/index.json (model metadata and entity index)
    /// - models/{modelName}/semanticmodel.json (main model document)
    /// - models/{modelName}/tables/{tableName}.json (individual table documents)
    /// - models/{modelName}/views/{viewName}.json (individual view documents)
    /// - models/{modelName}/storedprocedures/{procedureName}.json (individual stored procedure documents)
    /// 
    /// Security features:
    /// - Secure JSON serialization with injection protection
    /// - Azure Key Vault integration for credential management
    /// - Enhanced input validation and path security
    /// - Audit logging for all operations
    /// - Concurrent operation protection with semaphores
    /// 
    /// References:
    /// - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
    /// - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started
    /// - https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net
    /// </remarks>
    public class AzureBlobPersistenceStrategy : IAzureBlobPersistenceStrategy
    {
        #region Constants

        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 1000;
        private const int RetryDelayMultiplier = 2;
        private const int MaxConcurrentOperations = 10;
        private const string DefaultModelsPrefix = "models/";

        #endregion

        private BlobServiceClient? _blobServiceClient;
        private BlobContainerClient? _containerClient;
        private readonly AzureBlobConfiguration _configuration;
        private readonly ILogger<AzureBlobPersistenceStrategy> _logger;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly SemaphoreSlim _clientRefreshSemaphore = new SemaphoreSlim(1, 1);
        private readonly ISecureJsonSerializer _secureJsonSerializer;
        private readonly KeyVaultConfigurationProvider? _keyVaultProvider;

        /// <summary>
        /// Initializes a new instance of the AzureBlobPersistenceStrategy class with enhanced security features.
        /// </summary>
        /// <param name="configuration">Azure Blob Storage configuration options.</param>
        /// <param name="logger">Logger for structured logging.</param>
        /// <param name="secureJsonSerializer">Secure JSON serializer for injection protection.</param>
        /// <param name="keyVaultProvider">Optional Azure Key Vault configuration provider for secure credential management.</param>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public AzureBlobPersistenceStrategy(
            AzureBlobConfiguration configuration,
            ILogger<AzureBlobPersistenceStrategy> logger,
            ISecureJsonSerializer secureJsonSerializer,
            KeyVaultConfigurationProvider? keyVaultProvider = null,
            bool skipInitialization = false)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secureJsonSerializer = secureJsonSerializer ?? throw new ArgumentNullException(nameof(secureJsonSerializer));
            _keyVaultProvider = keyVaultProvider;

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

            if (!skipInitialization)
            {
                try
                {
                    // Enhanced credential management with Key Vault support (synchronous initialization)
                    (_blobServiceClient, _containerClient) = InitializeBlobServiceClient();

                    _logger.LogInformation("Initialized Azure Blob Storage persistence strategy with enhanced security features, " +
                        "endpoint {AccountEndpoint} and container {ContainerName}",
                        _configuration.AccountEndpoint, _configuration.ContainerName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Azure Blob Storage client with enhanced security features, " +
                        "endpoint {AccountEndpoint}",
                        _configuration.AccountEndpoint);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes the BlobServiceClient using Entra ID (DefaultAzureCredential). Connection strings are avoided.
        /// </summary>
        private (BlobServiceClient blobServiceClient, BlobContainerClient containerClient) InitializeBlobServiceClient()
        {
            try
            {
                var clientOptions = CreateBlobClientOptions();
                var credential = CreateDefaultCredential();
                var serviceClient = CreateBlobServiceClient(new Uri(_configuration.AccountEndpoint), credential, clientOptions);
                var containerClient = CreateBlobContainerClient(serviceClient, _configuration.ContainerName);

                _logger.LogInformation("Initialized BlobServiceClient using DefaultAzureCredential for endpoint {AccountEndpoint}", _configuration.AccountEndpoint);

                // Test the authentication by attempting to get container properties
                try
                {
                    var properties = containerClient.GetProperties();
                    _logger.LogDebug("Successfully authenticated to Azure Blob Storage container '{ContainerName}'", _configuration.ContainerName);
                }
                catch (Azure.RequestFailedException authEx) when (authEx.Status == 401)
                {
                    _logger.LogError(authEx,
                        "Authentication failed for Azure Blob Storage. This may indicate:" +
                        "\n1. Azure CLI/PowerShell signed in to different tenant than storage account" +
                        "\n2. Visual Studio/VS Code signed in to different tenant" +
                        "\n3. Multiple credential sources providing conflicting tokens" +
                        "\nStorage Account: {AccountEndpoint}" +
                        "\nContainer: {ContainerName}" +
                        "\nError Code: {ErrorCode}",
                        _configuration.AccountEndpoint, _configuration.ContainerName, authEx.ErrorCode);
                    throw;
                }

                return (serviceClient, containerClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure during BlobServiceClient initialization for endpoint {AccountEndpoint}", _configuration.AccountEndpoint);
                throw;
            }
        }

        /// <summary>
        /// Factory hook: create BlobClientOptions.
        /// </summary>
        protected virtual BlobClientOptions CreateBlobClientOptions() => new BlobClientOptions
        {
            Retry =
            {
                MaxRetries = 3,
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(30)
            }
        };

        /// <summary>
        /// Factory hook: create DefaultAzureCredential (or custom TokenCredential in tests).
        /// </summary>
        protected virtual TokenCredential CreateDefaultCredential()
        {
            var options = new DefaultAzureCredentialOptions
            {
                // Force to only use Azure CLI credential to avoid tenant conflicts
                ExcludeInteractiveBrowserCredential = true,
                ExcludeAzureCliCredential = false,          // Keep this enabled
                ExcludeManagedIdentityCredential = true,
                ExcludeVisualStudioCredential = true,       // Disable - may have different tenant
                ExcludeVisualStudioCodeCredential = true,   // Disable - may have different tenant
                ExcludeAzurePowerShellCredential = true,    // Disable - may have different tenant
                ExcludeEnvironmentCredential = true         // Disable - may have different tenant
            };

            var credential = new DefaultAzureCredential(options);
            _logger.LogDebug("Created DefaultAzureCredential restricted to Azure CLI only to avoid tenant conflicts");
            return credential;
        }

        /// <summary>
        /// Factory hook: create BlobServiceClient from endpoint + credential.
        /// </summary>
        protected virtual BlobServiceClient CreateBlobServiceClient(Uri endpoint, TokenCredential credential, BlobClientOptions options)
            => new BlobServiceClient(endpoint, credential, options);

        /// <summary>
        /// Factory hook: create BlobServiceClient from connection string (kept for extensibility but not used by default).
        /// </summary>
        protected virtual BlobServiceClient CreateBlobServiceClient(string connectionString, BlobClientOptions options)
            => new BlobServiceClient(connectionString, options);

        /// <summary>
        /// Factory hook: get/create BlobContainerClient for the configured container.
        /// </summary>
        protected virtual BlobContainerClient CreateBlobContainerClient(BlobServiceClient serviceClient, string containerName)
            => serviceClient.GetBlobContainerClient(containerName);

        /// <summary>
        /// Asynchronously attempts to retrieve and cache Azure Storage connection string from Key Vault.
        /// This method is called during operations to refresh credentials when needed.
        /// </summary>
        private async Task<string?> GetSecureConnectionStringAsync()
        {
            if (_keyVaultProvider == null)
            {
                return null;
            }

            try
            {
                _logger.LogTrace("Attempting to retrieve Azure Storage connection string from Key Vault");

                var connectionString = await _keyVaultProvider.GetConfigurationValueAsync(
                    "azure-storage-connection-string",
                    "AZURE_STORAGE_CONNECTION_STRING", // Environment variable fallback
                    null // No default value
                );

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogTrace("Successfully retrieved Azure Storage connection string from Key Vault");
                    return connectionString;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve connection string from Key Vault");
                return null;
            }
        }

        /// <summary>
        /// Refreshes the blob clients if Key Vault provides a connection string at runtime.
        /// This method is thread-safe and will not reinitialize clients concurrently.
        /// </summary>
        private async Task RefreshBlobClientsIfNeededAsync()
        {
            try
            {
                // If no key vault provider configured, nothing to do
                if (_keyVaultProvider == null) return;

                var connectionString = await GetSecureConnectionStringAsync().ConfigureAwait(false);

                await _clientRefreshSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var clientOptions = CreateBlobClientOptions();
                    BlobServiceClient newBlobServiceClient;
                    BlobContainerClient newContainerClient;
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        newBlobServiceClient = CreateBlobServiceClient(connectionString, clientOptions);
                        newContainerClient = CreateBlobContainerClient(newBlobServiceClient, _configuration.ContainerName);
                        _logger.LogInformation("Refreshed Blob clients from provided connection string");
                    }
                    else
                    {
                        var credential = CreateDefaultCredential();
                        newBlobServiceClient = CreateBlobServiceClient(new Uri(_configuration.AccountEndpoint), credential, clientOptions);
                        newContainerClient = CreateBlobContainerClient(newBlobServiceClient, _configuration.ContainerName);
                        _logger.LogInformation("Refreshed Blob clients using DefaultAzureCredential");
                    }

                    // Swap in the new instances
                    _blobServiceClient = newBlobServiceClient;
                    _containerClient = newContainerClient;
                }
                finally
                {
                    _clientRefreshSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh Blob clients");
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

            ValidateInputSecurity(semanticModel, modelPath);
            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting save operation for semantic model {ModelName} to Azure Blob Storage", modelName);

            try
            {
                await PrepareForSaveAsync();
                var blobPrefix = GetBlobPrefix(modelName);
                await SaveSemanticModelConcurrentlyAsync(semanticModel, blobPrefix);

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
        /// Prepares the environment for saving by refreshing clients and ensuring container exists.
        /// </summary>
        private async Task PrepareForSaveAsync()
        {
            await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
            await EnsureContainerExistsAsync();
        }

        /// <summary>
        /// Saves all components of the semantic model concurrently.
        /// </summary>
        /// <param name="semanticModel">The semantic model to save.</param>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        private async Task SaveSemanticModelConcurrentlyAsync(SemanticModel semanticModel, string blobPrefix)
        {
            // Get collections using async methods to support lazy loading
            var tables = await semanticModel.GetTablesAsync();
            var views = await semanticModel.GetViewsAsync();
            var storedProcedures = await semanticModel.GetStoredProceduresAsync();

            // Log entity counts for debugging
            _logger.LogInformation("Saving semantic model with {TableCount} tables, {ViewCount} views, and {StoredProcedureCount} stored procedures",
                tables.Count(), views.Count(), storedProcedures.Count());

            var concurrentTasks = new List<Task>();

            // Save main semantic model document
            var mainModelTask = SaveBlobAsync($"{blobPrefix}/semanticmodel.json", semanticModel, useEntityConverters: true);
            concurrentTasks.Add(mainModelTask);

            // Save entities concurrently
            AddEntitySaveTasks(concurrentTasks, tables, blobPrefix, "tables");
            AddEntitySaveTasks(concurrentTasks, views, blobPrefix, "views");
            AddEntitySaveTasks(concurrentTasks, storedProcedures, blobPrefix, "storedprocedures");

            await Task.WhenAll(concurrentTasks);
        }

        /// <summary>
        /// Adds save tasks for a collection of entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tasks">The task collection to add to.</param>
        /// <param name="entities">The entities to save.</param>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        /// <param name="entityType">The entity type folder name.</param>
        private void AddEntitySaveTasks<T>(List<Task> tasks, IEnumerable<T> entities, string blobPrefix, string entityType) where T : class
        {
            foreach (var entity in entities)
            {
                // Extract schema and name properties dynamically
                var schemaProperty = typeof(T).GetProperty("Schema");
                var nameProperty = typeof(T).GetProperty("Name");

                if (schemaProperty?.GetValue(entity) is string schema &&
                    nameProperty?.GetValue(entity) is string name)
                {
                    var sanitizedName = EntityNameSanitizer.SanitizeEntityName(name);
                    object persistedEntity = new LocalBlobEntityMapper().ToPersistedEntity(entity, null);
                    var saveTask = SaveBlobAsync($"{blobPrefix}/{entityType}/{schema}.{sanitizedName}.json", persistedEntity, useEntityConverters: false);
                    tasks.Add(saveTask);
                }
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

            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));
            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting load operation for semantic model {ModelName} from Azure Blob Storage", modelName);

            try
            {
                await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
                var blobPrefix = GetBlobPrefix(modelName);

                var semanticModel = await LoadMainSemanticModelAsync(blobPrefix, modelName);
                await LoadAllEntitiesConcurrentlyAsync(semanticModel, blobPrefix);

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
        /// Loads the main semantic model document from blob storage.
        /// </summary>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        /// <param name="modelName">The model name for logging.</param>
        /// <returns>The loaded semantic model.</returns>
        private async Task<SemanticModel> LoadMainSemanticModelAsync(string blobPrefix, string modelName)
        {
            var mainModelBlobName = $"{blobPrefix}/semanticmodel.json";

            if (_containerClient == null)
                throw new InvalidOperationException("Container client not initialized");

            var mainModelBlob = _containerClient.GetBlobClient(mainModelBlobName);

            if (!await mainModelBlob.ExistsAsync())
            {
                throw new FileNotFoundException($"Semantic model '{modelName}' not found in Azure Blob Storage.", mainModelBlobName);
            }

            var response = await DownloadContentAsync(mainModelBlob, CancellationToken.None);
            var jsonContent = response.Value.Content.ToString();

            // Create JSON serializer options with entity converters for proper deserialization
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelTableJsonConverter());
            jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelViewJsonConverter());
            jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelStoredProcedureJsonConverter());

            var semanticModel = await _secureJsonSerializer.DeserializeAsync<SemanticModel>(jsonContent, jsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize semantic model '{modelName}'.");

            _logger.LogInformation("Successfully loaded semantic model '{ModelName}' from Azure Blob Storage using secure JSON deserializer", modelName);

            return semanticModel;
        }

        /// <summary>
        /// Loads all entities (tables, views, stored procedures) concurrently.
        /// </summary>
        /// <param name="semanticModel">The semantic model containing the entities to load.</param>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        private async Task LoadAllEntitiesConcurrentlyAsync(SemanticModel semanticModel, string blobPrefix)
        {
            // Log entity counts for debugging
            _logger.LogInformation("Loading entities for semantic model: {TableCount} tables, {ViewCount} views, {StoredProcedureCount} stored procedures",
                semanticModel.Tables.Count, semanticModel.Views.Count, semanticModel.StoredProcedures.Count);

            var loadTasks = new List<Task>();

            // Load all entity types concurrently
            AddEntityLoadTasks(loadTasks, semanticModel.Tables, blobPrefix, "tables");
            AddEntityLoadTasks(loadTasks, semanticModel.Views, blobPrefix, "views");
            AddEntityLoadTasks(loadTasks, semanticModel.StoredProcedures, blobPrefix, "storedprocedures");

            _logger.LogInformation("Created {TaskCount} entity load tasks", loadTasks.Count);

            await Task.WhenAll(loadTasks);

            _logger.LogInformation("Completed loading all entity details");
        }

        /// <summary>
        /// Adds load tasks for a collection of entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tasks">The task collection to add to.</param>
        /// <param name="entities">The entities to load.</param>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        /// <param name="entityType">The entity type folder name.</param>
        private void AddEntityLoadTasks<T>(List<Task> tasks, IEnumerable<T> entities, string blobPrefix, string entityType) where T : class
        {
            int entityCount = 0;
            foreach (var entity in entities)
            {
                entityCount++;
                var loadTask = LoadEntityFromBlobAsync(entity, blobPrefix, entityType);
                tasks.Add(loadTask);
            }

            _logger.LogInformation("Added {EntityCount} {EntityType} load tasks", entityCount, entityType);
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

            // Validate model name input without requiring absolute filesystem path
            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));
            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);

            try
            {
                // Attempt to refresh clients if Key Vault provided a connection string at runtime
                await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
                var blobPrefix = GetBlobPrefix(modelName);
                var mainModelBlobName = $"{blobPrefix}/semanticmodel.json";
                if (_containerClient == null) throw new InvalidOperationException("Container client not initialized");
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
                // Attempt to refresh clients if Key Vault provided a connection string at runtime
                await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
                var modelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var prefix = string.IsNullOrWhiteSpace(_configuration.BlobPrefix)
                    ? DefaultModelsPrefix
                    : $"{_configuration.BlobPrefix.TrimEnd('/')}/{DefaultModelsPrefix}";

                if (_containerClient == null) throw new InvalidOperationException("Container client not initialized");
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

            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));
            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting delete operation for semantic model {ModelName} from Azure Blob Storage", modelName);

            try
            {
                // Attempt to refresh clients if Key Vault provided a connection string at runtime
                await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
                var blobPrefix = GetBlobPrefix(modelName);
                var deleteTasks = new List<Task>();

                // List all blobs with the model prefix
                if (_containerClient == null) throw new InvalidOperationException("Container client not initialized");
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
                if (_containerClient == null)
                {
                    _logger.LogWarning("Container client not initialized; skipping container existence check (tests may inject BlobClient directly)");
                    return;
                }
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
                ? $"{DefaultModelsPrefix}{sanitizedModelName}"
                : $"{_configuration.BlobPrefix.TrimEnd('/')}/{DefaultModelsPrefix}{sanitizedModelName}";
        }

        /// <summary>
        /// Saves a single object as a JSON blob with concurrency control.
        /// </summary>
        private async Task SaveBlobAsync<T>(string blobName, T data, bool useEntityConverters)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                // Create JSON serializer options for compatibility with existing format
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

                // Add entity converters for main model to avoid circular references
                if (useEntityConverters)
                {
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelTableJsonConverter());
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelViewJsonConverter());
                    jsonOptions.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelStoredProcedureJsonConverter());
                }

                // Use secure JSON serializer with audit logging for enhanced security
                var jsonContent = await _secureJsonSerializer.SerializeWithAuditAsync(
                    data,
                    $"AzureBlob:{blobName}",
                    jsonOptions).ConfigureAwait(false);

                var content = BinaryData.FromString(jsonContent ?? string.Empty);

                var blobClient = GetBlobClient(blobName);
                if (blobClient == null)
                {
                    _logger.LogWarning("BlobClient for {BlobName} is null. Ensure test injection or container client is set.", blobName);
                    return;
                }

                // Upload blob with retry logic
                await ExecuteWithRetryAsync(async () =>
                {
                    await blobClient.UploadAsync(content, overwrite: true).ConfigureAwait(false);
                    _logger.LogDebug("Saved blob {BlobName} ({Size} bytes) using secure JSON serializer",
                        blobName, content.ToArray().Length);
                }, $"UploadBlob:{blobName}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while saving blob {BlobName}", blobName);
                throw new InvalidOperationException($"Failed to save blob '{blobName}' to Azure Blob Storage.", ex);
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
                var blobClient = GetBlobClient(blobName);

                // Download blob with retry logic
                await ExecuteWithRetryAsync(async () =>
                {
                    var response = await DownloadContentAsync(blobClient, CancellationToken.None).ConfigureAwait(false);
                    if (response == null || response.Value.Content == null)
                    {
                        _logger.LogWarning("DownloadContentAsync returned no content for {BlobName}", blobName);
                        return;
                    }

                    var jsonContent = response.Value.Content.ToString();
                    await loadFromJsonFunc(jsonContent).ConfigureAwait(false);
                    _logger.LogDebug("Loaded entity from blob {BlobName}", blobName);
                }, $"DownloadBlob:{blobName}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while loading blob {BlobName}", blobName);
                throw new InvalidOperationException($"Failed to load blob '{blobName}' from Azure Blob Storage.", ex);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Wrapper for BlobClient.DownloadContentAsync to allow test overrides.
        /// </summary>
        /// <param name="blobClient">Blob client.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The blob download result response.</returns>
        protected virtual Task<Response<BlobDownloadResult>> DownloadContentAsync(BlobClient blobClient, CancellationToken cancellationToken)
            => blobClient.DownloadContentAsync(cancellationToken);

        /// <summary>
        /// Deletes a single blob with proper error handling.
        /// </summary>
        private async Task DeleteBlobAsync(BlobClient blobClient, string blobName)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                // Delete blob with retry logic
                await ExecuteWithRetryAsync(async () =>
                {
                    await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);
                    _logger.LogDebug("Deleted blob {BlobName}", blobName);
                }, $"DeleteBlob:{blobName}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while deleting blob {BlobName}", blobName);
                throw new InvalidOperationException($"Failed to delete blob '{blobName}' from Azure Blob Storage.", ex);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves a <see cref="BlobClient"/> for the given blob name. Protected virtual to allow testing overrides.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        protected virtual BlobClient GetBlobClient(string blobName)
        {
            if (_containerClient == null) throw new InvalidOperationException("Container client not initialized");
            return _containerClient.GetBlobClient(blobName);
        }

        private static bool IsTransientRequestFailure(RequestFailedException rfe)
        {
            // Treat common transient status codes as retryable
            return rfe.Status == 408 || rfe.Status == 429 || (rfe.Status >= 500 && rfe.Status < 600);
        }

        /// <summary>
        /// Extracts the JSON data from an envelope format { data, embedding } or returns the original JSON if no envelope is detected.
        /// </summary>
        /// <param name="jsonContent">The JSON content to process.</param>
        /// <returns>The extracted JSON data or the original content if no envelope is found.</returns>
        private static string ExtractJsonFromEnvelope(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || !jsonContent.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                return jsonContent;
            }

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                if (doc.RootElement.TryGetProperty("data", out var dataProp))
                {
                    return dataProp.GetRawText();
                }
            }
            catch
            {
                // If parsing fails, return original content
            }

            return jsonContent;
        }

        /// <summary>
        /// Writes JSON content to a file, extracting it from an envelope if present.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="jsonContent">The JSON content (possibly in envelope format).</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        private static async Task WriteJsonContentToFileAsync(string filePath, string jsonContent)
        {
            var extractedContent = ExtractJsonFromEnvelope(jsonContent);
            await File.WriteAllTextAsync(filePath, extractedContent);
        }

        /// <summary>
        /// Executes an async operation with exponential backoff retry for transient failures.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging purposes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
        {
            var delay = TimeSpan.FromMilliseconds(InitialRetryDelayMs);
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await operation().ConfigureAwait(false);
                    return;
                }
                catch (RequestFailedException rfe) when (IsTransientRequestFailure(rfe) && attempt < MaxRetries)
                {
                    _logger.LogWarning(rfe, "Transient failure in {Operation}, attempt {Attempt}. Retrying in {Delay}ms",
                        operationName, attempt, delay.TotalMilliseconds);
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * RetryDelayMultiplier);
                }
            }
        }

        /// <summary>
        /// Executes an async operation with return value and exponential backoff retry for transient failures.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging purposes.</param>
        /// <returns>A task representing the asynchronous operation with result.</returns>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var delay = TimeSpan.FromMilliseconds(InitialRetryDelayMs);
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (RequestFailedException rfe) when (IsTransientRequestFailure(rfe) && attempt < MaxRetries)
                {
                    _logger.LogWarning(rfe, "Transient failure in {Operation}, attempt {Attempt}. Retrying in {Delay}ms",
                        operationName, attempt, delay.TotalMilliseconds);
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * RetryDelayMultiplier);
                }
            }

            // This line should never be reached due to the throw in the last attempt
            throw new InvalidOperationException($"All retry attempts for {operationName} failed.");
        }

        /// <summary>
        /// Loads an entity (table, view, or stored procedure) from blob storage using a temporary directory approach.
        /// </summary>
        /// <typeparam name="T">The entity type that implements ILoadModelAsync.</typeparam>
        /// <param name="entity">The entity to load into.</param>
        /// <param name="blobPrefix">The blob prefix for the model.</param>
        /// <param name="entityType">The entity type folder name (e.g., "tables", "views", "storedprocedures").</param>
        /// <returns>A task representing the loading operation.</returns>
        private Task LoadEntityFromBlobAsync<T>(T entity, string blobPrefix, string entityType) where T : class
        {
            // Extract schema and name properties dynamically
            var schemaProperty = typeof(T).GetProperty("Schema");
            var nameProperty = typeof(T).GetProperty("Name");
            var loadModelMethod = typeof(T).GetMethod("LoadModelAsync");

            if (schemaProperty == null || nameProperty == null || loadModelMethod == null)
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} does not have required Schema, Name properties or LoadModelAsync method.");
            }

            var schema = schemaProperty.GetValue(entity)?.ToString() ?? string.Empty;
            var name = nameProperty.GetValue(entity)?.ToString() ?? string.Empty;
            var sanitizedName = EntityNameSanitizer.SanitizeEntityName(name);

            return LoadEntityAsync($"{blobPrefix}/{entityType}/{schema}.{sanitizedName}.json", async jsonContent =>
            {
                // Create temporary directory for entity loading
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                try
                {
                    // Use schema-qualified name for consistency
                    var filePath = Path.Combine(tempDir, $"{schema}.{sanitizedName}.json");
                    await WriteJsonContentToFileAsync(filePath, jsonContent);

                    // Invoke LoadModelAsync using reflection
                    var task = (Task?)loadModelMethod.Invoke(entity, new object[] { new DirectoryInfo(tempDir) });
                    if (task != null)
                    {
                        await task;
                    }
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            });
        }

        #endregion

        /// <summary>
        /// Loads the raw JSON content for a single entity blob. Unwraps envelope { data, embedding } when present.
        /// </summary>
        public async Task<string?> LoadEntityContentAsync(DirectoryInfo modelPath, string relativeEntityPath, CancellationToken cancellationToken)
        {
            if (modelPath == null) throw new ArgumentNullException(nameof(modelPath));
            if (string.IsNullOrWhiteSpace(relativeEntityPath)) throw new ArgumentNullException(nameof(relativeEntityPath));

            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));

            try
            {
                await RefreshBlobClientsIfNeededAsync().ConfigureAwait(false);
                var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
                var blobPrefix = GetBlobPrefix(modelName);
                var blobName = $"{blobPrefix}/{relativeEntityPath.TrimStart('/')}";

                if (_containerClient == null) throw new InvalidOperationException("Container client not initialized");
                var blobClient = GetBlobClient(blobName);

                // Download directly and handle 404 instead of calling Exists separately
                Response<BlobDownloadResult>? response;
                try
                {
                    response = await DownloadContentAsync(blobClient, cancellationToken).ConfigureAwait(false);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    return null;
                }
                if (response == null || response.Value.Content == null)
                {
                    return null;
                }

                var json = response.Value.Content.ToString();
                return ExtractJsonFromEnvelope(json);
            }
            catch (RequestFailedException ex) when (IsTransientRequestFailure(ex))
            {
                _logger.LogWarning(ex, "Transient failure while loading entity blob {RelativeEntityPath}", relativeEntityPath);
                throw;
            }
        }

        #region Input Validation

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

            if (!string.IsNullOrWhiteSpace(semanticModel.Source))
            {
                EntityNameSanitizer.ValidateInputSecurity(semanticModel.Source, nameof(semanticModel.Source));
            }

            // Validate path security
            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));

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

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the Azure Blob persistence strategy.
        /// </summary>
        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
            _clientRefreshSemaphore?.Dispose();
        }

        #endregion
    }
}
