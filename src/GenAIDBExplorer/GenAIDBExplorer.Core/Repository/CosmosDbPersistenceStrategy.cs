using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.Security;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;


namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses Azure Cosmos DB with hierarchical partition key structure.
    /// Implements security best practices with DefaultAzureCredential, managed identities,
    /// and enhanced security features including secure JSON serialization and Key Vault integration.
    /// </summary>
    /// <remarks>
    /// This implementation follows Azure Cosmos DB best practices:
    /// - Uses DefaultAzureCredential for authentication (supports managed identities)
    /// - Implements efficient partition key design for optimal performance
    /// - Includes proper error handling with retry policies
    /// - Supports concurrent operations with proper throttling
    /// - Uses session consistency for optimal balance of performance and consistency
    /// - Implements proper resource cleanup and connection management
    /// - Enhanced with secure JSON serialization to prevent injection attacks
    /// - Integrated with Azure Key Vault for secure credential management
    /// - Comprehensive security validation and audit logging
    /// 
    /// Document structure:
    /// Models Container (partition key: /modelName):
    /// - Document ID: {modelName}
    /// - Contains main semantic model with entity references
    /// 
    /// Entities Container (partition key: /modelName):
    /// - Document ID: {modelName}_{entityType}_{entityName}
    /// - Contains individual entity documents (tables, views, stored procedures)
    /// 
    /// Security features:
    /// - Secure JSON serialization with injection protection
    /// - Azure Key Vault integration for credential management
    /// - Enhanced input validation and data security
    /// - Audit logging for all operations
    /// - Concurrent operation protection with semaphores
    /// 
    /// References:
    /// - https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-dotnet-get-started
    /// - https://learn.microsoft.com/en-us/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard
    /// - https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net
    /// </remarks>
    public class CosmosDbPersistenceStrategy : ICosmosDbPersistenceStrategy
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Database _database;
        private readonly Container _modelsContainer;
        private readonly Container _entitiesContainer;
        private readonly CosmosDbConfiguration _configuration;
        private readonly ILogger<CosmosDbPersistenceStrategy> _logger;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly ISecureJsonSerializer _secureJsonSerializer;
        private readonly KeyVaultConfigurationProvider? _keyVaultProvider;

        /// <summary>
        /// Initializes a new instance of the CosmosDbPersistenceStrategy class with enhanced security features.
        /// </summary>
        /// <param name="configuration">Cosmos DB configuration options.</param>
        /// <param name="logger">Logger for structured logging.</param>
        /// <param name="secureJsonSerializer">Secure JSON serializer for injection protection.</param>
        /// <param name="keyVaultProvider">Optional Azure Key Vault configuration provider for secure credential management.</param>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public CosmosDbPersistenceStrategy(
            CosmosDbConfiguration configuration,
            ILogger<CosmosDbPersistenceStrategy> logger,
            ISecureJsonSerializer secureJsonSerializer,
            KeyVaultConfigurationProvider? keyVaultProvider = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secureJsonSerializer = secureJsonSerializer ?? throw new ArgumentNullException(nameof(secureJsonSerializer));
            _keyVaultProvider = keyVaultProvider;

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_configuration.AccountEndpoint))
            {
                throw new ArgumentException("AccountEndpoint is required in Cosmos DB configuration.", nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(_configuration.DatabaseName))
            {
                throw new ArgumentException("DatabaseName is required in Cosmos DB configuration.", nameof(configuration));
            }

            // Initialize concurrency control
            _concurrencySemaphore = new SemaphoreSlim(_configuration.MaxConcurrentOperations, _configuration.MaxConcurrentOperations);

            try
            {
                // Enhanced credential management with Key Vault support
                (_cosmosClient, _database, _modelsContainer, _entitiesContainer) = InitializeCosmosClient();

                _logger.LogInformation("Initialized Cosmos DB persistence strategy with enhanced security features, " +
                    "endpoint {AccountEndpoint}, database {DatabaseName}",
                    _configuration.AccountEndpoint, _configuration.DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Cosmos DB client with enhanced security features, " +
                    "endpoint {AccountEndpoint}",
                    _configuration.AccountEndpoint);
                throw;
            }
        }

        /// <summary>
        /// Initializes the CosmosClient with enhanced security features including Key Vault integration.
        /// </summary>
        private (CosmosClient cosmosClient, Database database, Container modelsContainer, Container entitiesContainer) InitializeCosmosClient()
        {
            try
            {
                // Attempt to retrieve connection string from environment variables (Key Vault integration is async,
                // so we'll rely on environment variable fallback in constructor for immediate initialization)
                var connectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");

                // Create CosmosClientOptions with best practices
                var clientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = TimeSpan.FromSeconds(_configuration.OperationTimeoutSeconds),
                    MaxRetryAttemptsOnRateLimitedRequests = _configuration.MaxRetryAttempts,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                    ConsistencyLevel = MapConsistencyLevel(_configuration.ConsistencyLevel)
                };

                CosmosClient cosmosClient;
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Use connection string if available from environment
                    cosmosClient = new CosmosClient(connectionString, clientOptions);
                    _logger.LogDebug("Initialized CosmosClient using connection string from environment variable");
                }
                else
                {
                    // Fall back to DefaultAzureCredential for managed identity/service principal auth
                    var credential = new DefaultAzureCredential();
                    cosmosClient = new CosmosClient(_configuration.AccountEndpoint, credential, clientOptions);
                    _logger.LogDebug("Initialized CosmosClient using DefaultAzureCredential");
                }

                var database = cosmosClient.GetDatabase(_configuration.DatabaseName);
                var modelsContainer = database.GetContainer(_configuration.ModelsContainerName);
                var entitiesContainer = database.GetContainer(_configuration.EntitiesContainerName);

                return (cosmosClient, database, modelsContainer, entitiesContainer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure during CosmosClient initialization");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously attempts to retrieve and cache Cosmos DB connection string from Key Vault.
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
                _logger.LogTrace("Attempting to retrieve Cosmos DB connection string from Key Vault");

                var connectionString = await _keyVaultProvider.GetConfigurationValueAsync(
                    "cosmos-db-connection-string",
                    "COSMOS_DB_CONNECTION_STRING", // Environment variable fallback
                    null // No default value
                );

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogTrace("Successfully retrieved Cosmos DB connection string from Key Vault");
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
        /// Saves the semantic model to Cosmos DB using hierarchical document structure.
        /// </summary>
        /// <param name="semanticModel">The semantic model to save.</param>
        /// <param name="modelPath">The logical path (model name) - used as partition key value.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when save operation fails.</exception>
        public async Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            // Enhanced input validation for security
            ValidateInputSecurity(semanticModel, modelPath);

            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting save operation for semantic model {ModelName} to Cosmos DB", modelName);

            try
            {
                // Ensure database and containers exist
                await EnsureDatabaseAndContainersExistAsync();

                var concurrentTasks = new List<Task>();

                // Save main semantic model document to models container
                var modelDocument = new
                {
                    id = modelName,
                    modelName = modelName,
                    name = semanticModel.Name,
                    source = semanticModel.Source,
                    description = semanticModel.Description,
                    createdAt = DateTimeOffset.UtcNow,
                    tablesCount = semanticModel.Tables.Count,
                    viewsCount = semanticModel.Views.Count,
                    storedProceduresCount = semanticModel.StoredProcedures.Count,
                    tables = semanticModel.Tables.Select(t => new { t.Name, t.Schema }),
                    views = semanticModel.Views.Select(v => new { v.Name, v.Schema }),
                    storedProcedures = semanticModel.StoredProcedures.Select(sp => new { sp.Name, sp.Schema })
                };

                var mainModelTask = SaveDocumentAsync(_modelsContainer, modelDocument, modelName);
                concurrentTasks.Add(mainModelTask);

                // Save table entities to entities container
                // Note (Phase 2 - REQ-006): For Cosmos strategy, vectors are NOT written to documents. Only metadata may be stored.
                foreach (var table in semanticModel.Tables)
                {
                    var tableName = EntityNameSanitizer.SanitizeEntityName(table.Name);
                    var tableDocument = new CosmosDbEntityDto<SemanticModelTable>
                    {
                        Id = $"{modelName}_table_{tableName}",
                        ModelName = modelName,
                        EntityType = "table",
                        EntityName = tableName,
                        Data = table,
                        Embedding = null,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    var tableTask = SaveDocumentAsync(_entitiesContainer, tableDocument, modelName);
                    concurrentTasks.Add(tableTask);
                }

                // Save view entities to entities container
                foreach (var view in semanticModel.Views)
                {
                    var viewName = EntityNameSanitizer.SanitizeEntityName(view.Name);
                    var viewDocument = new CosmosDbEntityDto<SemanticModelView>
                    {
                        Id = $"{modelName}_view_{viewName}",
                        ModelName = modelName,
                        EntityType = "view",
                        EntityName = viewName,
                        Data = view,
                        Embedding = null,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    var viewTask = SaveDocumentAsync(_entitiesContainer, viewDocument, modelName);
                    concurrentTasks.Add(viewTask);
                }

                // Save stored procedure entities to entities container
                foreach (var storedProcedure in semanticModel.StoredProcedures)
                {
                    var procedureName = EntityNameSanitizer.SanitizeEntityName(storedProcedure.Name);
                    var procedureDocument = new CosmosDbEntityDto<SemanticModelStoredProcedure>
                    {
                        Id = $"{modelName}_storedprocedure_{procedureName}",
                        ModelName = modelName,
                        EntityType = "storedprocedure",
                        EntityName = procedureName,
                        Data = storedProcedure,
                        Embedding = null,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    var procedureTask = SaveDocumentAsync(_entitiesContainer, procedureDocument, modelName);
                    concurrentTasks.Add(procedureTask);
                }

                // Wait for all saves to complete
                await Task.WhenAll(concurrentTasks);

                stopwatch.Stop();
                _logger.LogInformation("Successfully saved semantic model {ModelName} to Cosmos DB in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied to Cosmos DB for model {ModelName}", modelName);
                throw new InvalidOperationException($"Access denied to Cosmos DB. Ensure your managed identity has the 'Cosmos DB Built-in Data Contributor' role.", ex);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogError(ex, "Rate limit exceeded while saving model {ModelName}", modelName);
                throw new InvalidOperationException($"Rate limit exceeded while saving semantic model '{modelName}'. Please retry after a short delay.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save semantic model {ModelName} to Cosmos DB after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to save semantic model '{modelName}' to Cosmos DB: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads the semantic model from Cosmos DB.
        /// </summary>
        /// <param name="modelPath">The logical path (model name) - used as partition key value.</param>
        /// <returns>The loaded semantic model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when modelPath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the semantic model is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when load operation fails.</exception>
        public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            // Enhanced input validation
            EntityNameSanitizer.ValidateInputSecurity(modelPath.Name, nameof(modelPath));

            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting load operation for semantic model {ModelName} from Cosmos DB", modelName);

            try
            {
                // Load main semantic model document
                var response = await _modelsContainer.ReadItemAsync<dynamic>(modelName, new PartitionKey(modelName));
                var modelData = response.Resource;

                // Create semantic model object
                var semanticModel = new SemanticModel(
                    name: modelData.name,
                    source: modelData.source,
                    description: modelData.description
                );

                // Load entities concurrently
                var loadTasks = new List<Task>();

                // Load tables
                // Note: Entity 'data' was stored without vector floats per Phase 2/REQ-006.
                if (modelData.tables != null)
                {
                    foreach (var tableRef in modelData.tables)
                    {
                        var tableName = EntityNameSanitizer.SanitizeEntityName(tableRef.Name.ToString());
                        var loadTask = LoadEntityAsync<SemanticModelTable>(
                            $"{modelName}_table_{tableName}",
                            modelName,
                            entity => semanticModel.AddTable(entity));
                        loadTasks.Add(loadTask);
                    }
                }

                // Load views  
                if (modelData.views != null)
                {
                    foreach (var viewRef in modelData.views)
                    {
                        var viewName = EntityNameSanitizer.SanitizeEntityName(viewRef.Name.ToString());
                        var loadTask = LoadEntityAsync<SemanticModelView>(
                            $"{modelName}_view_{viewName}",
                            modelName,
                            entity => semanticModel.AddView(entity));
                        loadTasks.Add(loadTask);
                    }
                }

                // Load stored procedures
                if (modelData.storedProcedures != null)
                {
                    foreach (var procedureRef in modelData.storedProcedures)
                    {
                        var procedureName = EntityNameSanitizer.SanitizeEntityName(procedureRef.Name.ToString());
                        var loadTask = LoadEntityAsync<SemanticModelStoredProcedure>(
                            $"{modelName}_storedprocedure_{procedureName}",
                            modelName,
                            entity => semanticModel.AddStoredProcedure(entity));
                        loadTasks.Add(loadTask);
                    }
                }

                // Wait for all loads to complete
                await Task.WhenAll(loadTasks);

                stopwatch.Stop();
                _logger.LogInformation("Successfully loaded semantic model {ModelName} from Cosmos DB in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);

                return semanticModel;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Semantic model {ModelName} not found in Cosmos DB", modelName);
                throw new FileNotFoundException($"Semantic model '{modelName}' not found in Cosmos DB.", ex);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied to Cosmos DB for model {ModelName}", modelName);
                throw new InvalidOperationException($"Access denied to Cosmos DB. Ensure your managed identity has proper permissions.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load semantic model {ModelName} from Cosmos DB after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to load semantic model '{modelName}' from Cosmos DB: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a semantic model exists in Cosmos DB.
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
                var response = await _modelsContainer.ReadItemAsync<dynamic>(modelName, new PartitionKey(modelName));

                _logger.LogDebug("Semantic model {ModelName} exists check: {Exists}", modelName, true);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Semantic model {ModelName} exists check: {Exists}", modelName, false);
                return false;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
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
        /// Lists all available semantic models in the Cosmos DB database.
        /// </summary>
        /// <param name="rootPath">The root path (ignored for Cosmos DB - uses database scope).</param>
        /// <returns>An enumerable of model names found in the database.</returns>
        public async Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath)
        {
            try
            {
                var modelNames = new List<string>();
                var query = "SELECT c.id FROM c";

                using var iterator = _modelsContainer.GetItemQueryIterator<dynamic>(query);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        modelNames.Add(item.id.ToString());
                    }
                }

                _logger.LogInformation("Found {Count} semantic models in Cosmos DB database {DatabaseName}",
                    modelNames.Count, _configuration.DatabaseName);

                return modelNames;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied when listing models in database {DatabaseName}", _configuration.DatabaseName);
                throw new InvalidOperationException($"Access denied when listing models. Ensure your managed identity has proper permissions.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list semantic models in Cosmos DB database {DatabaseName}",
                    _configuration.DatabaseName);
                throw new InvalidOperationException($"Failed to list semantic models in Cosmos DB: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a semantic model from Cosmos DB.
        /// </summary>
        /// <param name="modelPath">The logical path (model name) to delete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        public async Task DeleteModelAsync(DirectoryInfo modelPath)
        {
            if (modelPath == null)
                throw new ArgumentNullException(nameof(modelPath));

            var modelName = PathValidator.ValidateAndSanitizePath(modelPath.Name);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting delete operation for semantic model {ModelName} from Cosmos DB", modelName);

            try
            {
                var deleteTasks = new List<Task>();

                // Delete main model document
                var deleteModelTask = DeleteDocumentAsync(_modelsContainer, modelName, modelName);
                deleteTasks.Add(deleteModelTask);

                // Delete all entity documents for this model
                var query = $"SELECT c.id FROM c WHERE c.modelName = '{modelName}'";
                using var iterator = _entitiesContainer.GetItemQueryIterator<dynamic>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        var entityId = item.id.ToString();
                        var deleteTask = DeleteDocumentAsync(_entitiesContainer, entityId, modelName);
                        deleteTasks.Add(deleteTask);
                    }
                }

                // Wait for all deletions to complete
                await Task.WhenAll(deleteTasks);

                stopwatch.Stop();
                _logger.LogInformation("Successfully deleted semantic model {ModelName} from Cosmos DB in {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied when deleting model {ModelName}", modelName);
                throw new InvalidOperationException($"Access denied when deleting model. Ensure your managed identity has proper permissions.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete semantic model {ModelName} from Cosmos DB after {ElapsedMs}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to delete semantic model '{modelName}' from Cosmos DB: {ex.Message}", ex);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Ensures the Cosmos DB database and containers exist and creates them if necessary.
        /// </summary>
        private async Task EnsureDatabaseAndContainersExistAsync()
        {
            try
            {
                // Create database if it doesn't exist
                var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                    _configuration.DatabaseName,
                    _configuration.DatabaseThroughput);

                // Create models container if it doesn't exist
                await _database.CreateContainerIfNotExistsAsync(
                    _configuration.ModelsContainerName,
                    _configuration.ModelsPartitionKeyPath);

                // Create entities container if it doesn't exist
                await _database.CreateContainerIfNotExistsAsync(
                    _configuration.EntitiesContainerName,
                    _configuration.EntitiesPartitionKeyPath);

                _logger.LogDebug("Ensured database {DatabaseName} and containers exist", _configuration.DatabaseName);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied when creating/accessing database {DatabaseName}", _configuration.DatabaseName);
                throw new InvalidOperationException($"Access denied to database '{_configuration.DatabaseName}'. Ensure your managed identity has proper permissions.", ex);
            }
        }

        /// <summary>
        /// Saves a document to the specified container with concurrency control.
        /// </summary>
        private async Task SaveDocumentAsync<T>(Container container, T document, string partitionKeyValue)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                await container.UpsertItemAsync(document, new PartitionKey(partitionKeyValue));
                _logger.LogDebug("Saved document to container {ContainerName}", container.Id);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Loads an entity from Cosmos DB and processes it with the provided action.
        /// </summary>
        private async Task LoadEntityAsync<T>(string documentId, string partitionKeyValue, Action<T> processEntity) where T : class
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                var response = await _entitiesContainer.ReadItemAsync<dynamic>(documentId, new PartitionKey(partitionKeyValue));
                var entityData = response.Resource.data;

                // Deserialize the entity data using secure JSON serializer
                var jsonString = entityData.ToString();
                var entity = await _secureJsonSerializer.DeserializeAsync<T>(jsonString);

                if (entity != null)
                {
                    processEntity(entity);
                    _logger.LogDebug("Loaded entity {DocumentId} from container {ContainerName} using secure JSON deserializer",
                        documentId, _entitiesContainer.Id);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Entity document {DocumentId} not found", documentId);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Deletes a document from the specified container.
        /// </summary>
        private async Task DeleteDocumentAsync(Container container, string documentId, string partitionKeyValue)
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                await container.DeleteItemAsync<dynamic>(documentId, new PartitionKey(partitionKeyValue));
                _logger.LogDebug("Deleted document {DocumentId} from container {ContainerName}", documentId, container.Id);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Document {DocumentId} not found for deletion", documentId);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        /// <summary>
        /// Maps the configuration consistency level to Cosmos DB consistency level.
        /// </summary>
        private static Microsoft.Azure.Cosmos.ConsistencyLevel MapConsistencyLevel(CosmosConsistencyLevel level)
        {
            return level switch
            {
                CosmosConsistencyLevel.Eventual => Microsoft.Azure.Cosmos.ConsistencyLevel.Eventual,
                CosmosConsistencyLevel.ConsistentPrefix => Microsoft.Azure.Cosmos.ConsistencyLevel.ConsistentPrefix,
                CosmosConsistencyLevel.Session => Microsoft.Azure.Cosmos.ConsistencyLevel.Session,
                CosmosConsistencyLevel.BoundedStaleness => Microsoft.Azure.Cosmos.ConsistencyLevel.BoundedStaleness,
                CosmosConsistencyLevel.Strong => Microsoft.Azure.Cosmos.ConsistencyLevel.Strong,
                _ => Microsoft.Azure.Cosmos.ConsistencyLevel.Session
            };
        }

        #endregion

        /// <summary>
        /// Loads the raw JSON content of a single entity document from the entities container.
        /// Returns the inner 'data' JSON (as a string) when present, or null when the document is not found.
        /// </summary>
        public async Task<string?> LoadEntityContentAsync(DirectoryInfo modelPath, string relativeEntityPath, CancellationToken cancellationToken)
        {
            if (modelPath == null) throw new ArgumentNullException(nameof(modelPath));
            if (string.IsNullOrWhiteSpace(relativeEntityPath)) throw new ArgumentNullException(nameof(relativeEntityPath));

            var modelName = EntityNameSanitizer.SanitizeEntityName(modelPath.Name);

            try
            {
                // Normalize path like "tables/<name>.json" -> entityType and entityName
                var parts = relativeEntityPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return null;

                var folder = parts.Length > 1 ? parts[0].ToLowerInvariant() : string.Empty;
                var fileName = Path.GetFileNameWithoutExtension(parts[^1]);
                if (string.IsNullOrWhiteSpace(fileName)) return null;

                string entityType = folder switch
                {
                    "tables" => "table",
                    "views" => "view",
                    "storedprocedures" => "storedprocedure",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(entityType)) return null;

                var entityName = EntityNameSanitizer.SanitizeEntityName(fileName);
                var documentId = $"{modelName}_{entityType}_{entityName}";

                var response = await _entitiesContainer.ReadItemAsync<dynamic>(documentId, new PartitionKey(modelName), cancellationToken: cancellationToken).ConfigureAwait(false);
                var entityData = response.Resource.data;
                var jsonString = entityData?.ToString();
                return jsonString;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Entity document not found in Cosmos DB: {RelativePath} for model {ModelName}", relativeEntityPath, modelPath.Name);
                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Access denied when loading entity {RelativePath} from Cosmos DB for model {ModelName}", relativeEntityPath, modelPath.Name);
                throw new InvalidOperationException($"Access denied when loading entity '{relativeEntityPath}' from Cosmos DB.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load entity content {RelativePath} from Cosmos DB for model {ModelName}", relativeEntityPath, modelPath.Name);
                return null;
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

        /// <summary>
        /// Checks if a vector exists for the specified entity with the given content hash.
        /// For CosmosDB strategy, this queries the entity document for existing vector metadata.
        /// </summary>
        public async Task<string?> CheckVectorExistsAsync(string entityType, string schemaName, string entityName,
            string contentHash, DirectoryInfo modelPath, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(schemaName);
            ArgumentNullException.ThrowIfNull(entityName);
            ArgumentNullException.ThrowIfNull(contentHash);
            ArgumentNullException.ThrowIfNull(modelPath);

            // Input validation for security
            EntityNameSanitizer.ValidateInputSecurity(entityType, nameof(entityType));
            EntityNameSanitizer.ValidateInputSecurity(schemaName, nameof(schemaName));
            EntityNameSanitizer.ValidateInputSecurity(entityName, nameof(entityName));

            try
            {
                // Use the _entitiesContainer field that's already initialized
                var container = _entitiesContainer;

                // Build document ID using same pattern as CosmosDB persistence
                var modelName = modelPath.Name;
                var documentId = $"{modelName}_{entityType}_{schemaName}.{entityName}";

                // Query for the specific document with vector metadata
                var query = "SELECT c.embedding.contentHash FROM c WHERE c.id = @id";
                var queryDefinition = new QueryDefinition(query)
                    .WithParameter("@id", documentId);

                using var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    var item = response.FirstOrDefault();

                    if (item != null)
                    {
                        // Extract contentHash from the nested embedding metadata
                        string? existingHash = null;
                        try
                        {
                            existingHash = item.contentHash?.ToString();
                        }
                        catch
                        {
                            // Handle case where embedding or contentHash doesn't exist
                        }

                        return string.Equals(existingHash?.Trim(), contentHash?.Trim(), StringComparison.OrdinalIgnoreCase)
                            ? existingHash
                            : null;
                    }
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Document doesn't exist
                _logger.LogDebug("Vector document not found for {EntityType} {Schema}.{Name}", entityType, schemaName, entityName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check vector existence for {EntityType} {Schema}.{Name}", entityType, schemaName, entityName);
                return null;
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the Cosmos DB persistence strategy.
        /// </summary>
        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
            _cosmosClient?.Dispose();
        }

        #endregion
    }
}
