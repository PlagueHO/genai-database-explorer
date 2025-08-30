using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Resources;
using System.IO;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public sealed class SemanticModelProvider(
    IProject project,
    ISchemaRepository schemaRepository,
    ILogger<SemanticModelProvider> logger,
    ISemanticModelRepository semanticModelRepository
) : ISemanticModelProvider
{
    private readonly IProject _project = project ?? throw new ArgumentNullException(nameof(project));
    private readonly ISchemaRepository _schemaRepository = schemaRepository ?? throw new ArgumentNullException(nameof(schemaRepository));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISemanticModelRepository _semanticModelRepository = semanticModelRepository ?? throw new ArgumentNullException(nameof(semanticModelRepository));
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SemanticModelProvider).Assembly);

    /// <inheritdoc/>
    public SemanticModel CreateSemanticModel()
    {
        // Create the new SemanticModel instance to build
        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> LoadSemanticModelAsync()
    {
        var persistenceStrategy = _project.Settings.SemanticModel.PersistenceStrategy;
        _logger.LogInformation("{Message} using strategy '{PersistenceStrategy}'", _resourceManagerLogMessages.GetString("LoadingSemanticModel"), persistenceStrategy);

        SemanticModel semanticModel;

        switch (persistenceStrategy)
        {
            case "LocalDisk":
                semanticModel = await LoadSemanticModelFromLocalDiskAsync();
                break;

            case "AzureBlob":
                semanticModel = await LoadSemanticModelFromAzureBlobAsync();
                break;

            case "CosmosDb":
                throw new NotSupportedException($"Persistence strategy '{persistenceStrategy}' is not yet supported for loading semantic models.");

            default:
                throw new ArgumentException($"Unknown persistence strategy '{persistenceStrategy}' specified in project settings.", nameof(persistenceStrategy));
        }

        _logger.LogInformation("{Message} '{SemanticModelName}' using strategy '{PersistenceStrategy}'", _resourceManagerLogMessages.GetString("LoadedSemanticModelForDatabase"), semanticModel.Name, persistenceStrategy);

        return semanticModel;
    }

    /// <summary>
    /// Loads the semantic model from local disk using the configured directory path with settings-driven lazy loading and caching.
    /// </summary>
    /// <returns>The loaded semantic model.</returns>
    private async Task<SemanticModel> LoadSemanticModelFromLocalDiskAsync()
    {
        // Get the configured directory for LocalDisk storage
        var localDiskConfig = _project.Settings.SemanticModelRepository?.LocalDisk;
        if (localDiskConfig?.Directory == null || string.IsNullOrWhiteSpace(localDiskConfig.Directory))
        {
            throw new InvalidOperationException("LocalDisk persistence strategy is configured but no directory is specified in SemanticModelRepository.LocalDisk.Directory.");
        }

        // Build the full path using the project's working directory and configured semantic model directory
        var projectDirectory = _project.ProjectDirectory;
        var semanticModelPath = new DirectoryInfo(Path.Combine(projectDirectory.FullName, localDiskConfig.Directory));

        var repositorySettings = _project.Settings.SemanticModelRepository!;
        _logger.LogDebug("Loading semantic model from local disk path: '{SemanticModelPath}' with settings-driven configuration (LazyLoading: {LazyLoading}, Caching: {Caching}, ChangeTracking: {ChangeTracking})",
            semanticModelPath.FullName,
            repositorySettings.LazyLoading.Enabled,
            repositorySettings.Caching.Enabled,
            repositorySettings.ChangeTracking.Enabled);

        // Create repository options using the builder pattern controlled by settings
        var optionsBuilder = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(repositorySettings.LazyLoading.Enabled)
            .WithChangeTracking(repositorySettings.ChangeTracking.Enabled)
            .WithCaching(repositorySettings.Caching.Enabled, TimeSpan.FromMinutes(repositorySettings.Caching.ExpirationMinutes))
            .WithMaxConcurrentOperations(repositorySettings.MaxConcurrentOperations)
            .WithStrategyName("LocalDisk");

        // Add performance monitoring if enabled
        if (repositorySettings.PerformanceMonitoring.Enabled)
        {
            optionsBuilder = optionsBuilder.WithPerformanceMonitoring(builder =>
                builder.EnableLocalMonitoring(true)
                       .WithMetricsRetention(TimeSpan.FromHours(24)));
        }

        var options = optionsBuilder.Build();

        // Use the repository for loading semantic models with settings-driven options
        try
        {
            return await _semanticModelRepository.LoadModelAsync(semanticModelPath, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load semantic model using repository from '{SemanticModelPath}', attempting direct load or fallback to empty", semanticModelPath.FullName);
            try
            {
                return await CreateSemanticModel().LoadModelAsync(semanticModelPath);
            }
            catch (FileNotFoundException)
            {
                return CreateSemanticModel();
            }
            catch (DirectoryNotFoundException)
            {
                return CreateSemanticModel();
            }
        }
    }

    /// <summary>
    /// Loads the semantic model from Azure Blob Storage using the configured storage account and container with settings-driven lazy loading and caching.
    /// </summary>
    /// <returns>The loaded semantic model.</returns>
    private async Task<SemanticModel> LoadSemanticModelFromAzureBlobAsync()
    {
        // Get the configured Azure Blob Storage settings
        var azureBlobConfig = _project.Settings.SemanticModelRepository?.AzureBlobStorage;
        if (azureBlobConfig?.AccountEndpoint == null || string.IsNullOrWhiteSpace(azureBlobConfig.AccountEndpoint))
        {
            throw new InvalidOperationException("AzureBlob persistence strategy is configured but no AccountEndpoint is specified in SemanticModelRepository.AzureBlobStorage.AccountEndpoint.");
        }

        if (azureBlobConfig?.ContainerName == null || string.IsNullOrWhiteSpace(azureBlobConfig.ContainerName))
        {
            throw new InvalidOperationException("AzureBlob persistence strategy is configured but no ContainerName is specified in SemanticModelRepository.AzureBlobStorage.ContainerName.");
        }

        // Create a logical directory path using the model name for the Azure Blob persistence strategy
        var modelName = _project.Settings.Database.Name;
        var logicalModelPath = new DirectoryInfo(modelName);

        var repositorySettings = _project.Settings.SemanticModelRepository!;
        _logger.LogDebug("Loading semantic model from Azure Blob Storage (Account: '{AccountEndpoint}', Container: '{ContainerName}') with settings-driven configuration (LazyLoading: {LazyLoading}, Caching: {Caching}, ChangeTracking: {ChangeTracking})",
            azureBlobConfig.AccountEndpoint,
            azureBlobConfig.ContainerName,
            repositorySettings.LazyLoading.Enabled,
            repositorySettings.Caching.Enabled,
            repositorySettings.ChangeTracking.Enabled);

        // Create repository options using the builder pattern controlled by settings
        var optionsBuilder = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(repositorySettings.LazyLoading.Enabled)
            .WithChangeTracking(repositorySettings.ChangeTracking.Enabled)
            .WithCaching(repositorySettings.Caching.Enabled, TimeSpan.FromMinutes(repositorySettings.Caching.ExpirationMinutes))
            .WithMaxConcurrentOperations(repositorySettings.MaxConcurrentOperations)
            .WithStrategyName("AzureBlob");

        // Add performance monitoring if enabled
        if (repositorySettings.PerformanceMonitoring.Enabled)
        {
            optionsBuilder = optionsBuilder.WithPerformanceMonitoring(builder =>
                builder.EnableLocalMonitoring(true)
                       .WithMetricsRetention(TimeSpan.FromHours(24)));
        }

        var options = optionsBuilder.Build();

        // Use the repository for loading semantic models with settings-driven options
        try
        {
            return await _semanticModelRepository.LoadModelAsync(logicalModelPath, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load semantic model using repository from Azure Blob Storage (model: '{ModelName}'), attempting fallback to empty", modelName);
            try
            {
                // For Azure Blob, we can't fall back to direct load like LocalDisk since the persistence strategy handles the storage details
                // Instead, we return a new semantic model as fallback
                return CreateSemanticModel();
            }
            catch (FileNotFoundException)
            {
                return CreateSemanticModel();
            }
            catch (DirectoryNotFoundException)
            {
                return CreateSemanticModel();
            }
        }
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath)
    {
        var repositorySettings = _project.Settings.SemanticModelRepository!;
        _logger.LogInformation("{Message} '{ModelPath}' with settings-driven configuration (LazyLoading: {LazyLoading}, Caching: {Caching}, ChangeTracking: {ChangeTracking})",
            _resourceManagerLogMessages.GetString("LoadingSemanticModel"),
            modelPath,
            repositorySettings.LazyLoading.Enabled,
            repositorySettings.Caching.Enabled,
            repositorySettings.ChangeTracking.Enabled);

        SemanticModel semanticModel;

        // Create repository options using the builder pattern controlled by settings
        var optionsBuilder = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(repositorySettings.LazyLoading.Enabled)
            .WithChangeTracking(repositorySettings.ChangeTracking.Enabled)
            .WithCaching(repositorySettings.Caching.Enabled, TimeSpan.FromMinutes(repositorySettings.Caching.ExpirationMinutes))
            .WithMaxConcurrentOperations(repositorySettings.MaxConcurrentOperations)
            .WithStrategyName(_project.Settings.SemanticModel.PersistenceStrategy);

        // Add performance monitoring if enabled
        if (repositorySettings.PerformanceMonitoring.Enabled)
        {
            optionsBuilder = optionsBuilder.WithPerformanceMonitoring(builder =>
                builder.EnableLocalMonitoring(true)
                       .WithMetricsRetention(TimeSpan.FromHours(24)));
        }

        var options = optionsBuilder.Build();

        // Use the repository for loading semantic models with settings-driven options
        _logger.LogDebug("Using SemanticModelRepository to load semantic model from '{ModelPath}' with options: LazyLoading={EnableLazyLoading}, ChangeTracking={EnableChangeTracking}, Caching={EnableCaching}",
            modelPath, options.EnableLazyLoading, options.EnableChangeTracking, options.EnableCaching);
        try
        {
            semanticModel = await _semanticModelRepository.LoadModelAsync(modelPath, options);
        }
        catch (Exception ex) when (ex is not FileNotFoundException && ex is not DirectoryNotFoundException)
        {
            _logger.LogWarning(ex, "Failed to load semantic model using repository, attempting direct load or fallback to empty");
            try
            {
                semanticModel = await CreateSemanticModel().LoadModelAsync(modelPath);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
        }

        _logger.LogInformation("{Message} '{SemanticModelName}' (LazyLoading: {IsLazyLoadingEnabled}, ChangeTracking: {IsChangeTrackingEnabled})",
            _resourceManagerLogMessages.GetString("LoadedSemanticModelForDatabase"),
            semanticModel.Name,
            semanticModel.IsLazyLoadingEnabled,
            semanticModel.IsChangeTrackingEnabled);

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> ExtractSemanticModelAsync()
    {
        _logger.LogInformation("{Message} '{DatabaseName}'", _resourceManagerLogMessages.GetString("ExtractingModelForDatabase"), _project.Settings.Database.Name);

        // Create the new SemanticModel instance to build
        var semanticModel = CreateSemanticModel();

        // Configure the parallel parallelOptions for the operation
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _project.Settings.Database.MaxDegreeOfParallelism
        };

        await ExtractSemanticModelTablesAsync(semanticModel, parallelOptions);

        await ExtractSemanticModelViewsAsync(semanticModel, parallelOptions);

        await ExtractSemanticModelStoredProceduresAsync(semanticModel, parallelOptions);

        // return the semantic model Task
        return semanticModel;
    }

    /// <summary>
    /// Extracts the tables from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the tables will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelTablesAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the tables from the database
        var tablesDictionary = await _schemaRepository.GetTablesAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelTables = new ConcurrentBag<SemanticModelTable>();

        // Construct the semantic model tables
        await Parallel.ForEachAsync(tablesDictionary.Values, parallelOptions, async (table, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("AddingTableToSemanticModel"), table.SchemaName, table.TableName);

            var semanticModelTable = await _schemaRepository.CreateSemanticModelTableAsync(table).ConfigureAwait(false);
            semanticModelTables.Add(semanticModelTable);
        });

        // Add the tables to the semantic model
        semanticModel.Tables.AddRange(semanticModelTables);
    }

    /// <summary>
    /// Extracts the views from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the views will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelViewsAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the views from the database
        var viewsDictionary = await _schemaRepository.GetViewsAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelViews = new ConcurrentBag<SemanticModelView>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(viewsDictionary.Values, parallelOptions, async (view, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("AddingViewToSemanticModel"), view.SchemaName, view.ViewName);

            var semanticModelView = await _schemaRepository.CreateSemanticModelViewAsync(view).ConfigureAwait(false);
            semanticModelViews.Add(semanticModelView);
        });

        // Add the view to the semantic model
        semanticModel.Views.AddRange(semanticModelViews);
    }

    /// <summary>
    /// Extracts the stored procedures from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the stored procedures will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelStoredProceduresAsync(SemanticModel semanticModel, ParallelOptions parallelOptions)
    {
        // Get the stored procedures from the database
        var storedProceduresDictionary = await _schemaRepository.GetStoredProceduresAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelStoredProcedures = new ConcurrentBag<SemanticModelStoredProcedure>();

        // Construct the semantic model views
        await Parallel.ForEachAsync(storedProceduresDictionary.Values, parallelOptions, async (storedProcedure, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("AddingStoredProcedureToSemanticModel"), storedProcedure.SchemaName, storedProcedure.ProcedureName);

            var semanticModeStoredProcedure = await _schemaRepository.CreateSemanticModelStoredProcedureAsync(storedProcedure).ConfigureAwait(false);
            semanticModelStoredProcedures.Add(semanticModeStoredProcedure);
        });

        // Add the stored procedures to the semantic model
        semanticModel.StoredProcedures.AddRange(semanticModelStoredProcedures);
    }
}