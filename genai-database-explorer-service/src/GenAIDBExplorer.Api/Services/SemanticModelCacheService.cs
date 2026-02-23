using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;

namespace GenAIDBExplorer.Api.Services;

/// <summary>
/// Thread-safe in-memory cache for the semantic model using atomic reference swap.
/// </summary>
public class SemanticModelCacheService(
    IProject project,
    ISemanticModelRepository repository,
    ILogger<SemanticModelCacheService> logger
) : ISemanticModelCacheService
{
    private volatile SemanticModel? _cachedModel;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    /// <inheritdoc />
    public bool IsLoaded => _cachedModel != null;

    /// <inheritdoc />
    public async Task<SemanticModel> GetModelAsync()
    {
        var model = _cachedModel;
        if (model != null)
        {
            return model;
        }

        return await LoadModelCoreAsync();
    }

    /// <inheritdoc />
    public async Task<SemanticModel> ReloadModelAsync()
    {
        logger.LogInformation("Reloading semantic model from repository");
        return await LoadModelCoreAsync();
    }

    private async Task<SemanticModel> LoadModelCoreAsync()
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            var modelPath = project.GetSemanticModelPath();

            var options = new SemanticModelRepositoryOptions
            {
                EnableChangeTracking = true
            };

            var newModel = await repository.LoadModelAsync(modelPath, options);

            // Atomic swap — in-flight reads continue using the old reference
            Interlocked.Exchange(ref _cachedModel, newModel);

            logger.LogInformation(
                "Semantic model loaded: {ModelName} ({TableCount} tables, {ViewCount} views, {SprocCount} stored procedures)",
                newModel.Name,
                newModel.Tables.Count,
                newModel.Views.Count,
                newModel.StoredProcedures.Count);

            return newModel;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load semantic model");
            throw;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }
}
