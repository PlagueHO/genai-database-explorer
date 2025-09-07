using System.IO;
using System.Text;
using System.Text.Json;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Helpers;

/// <summary>
/// Manages entity file operations for semantic model persistence.
/// </summary>
internal class EntityFileManager
{
    private readonly ISecureJsonSerializer _secureJsonSerializer;
    private readonly ILogger _logger;
    private readonly LocalBlobEntityMapper _mapper;
    private readonly JsonSerializerOptions _entityJsonOptions;

    public EntityFileManager(ISecureJsonSerializer secureJsonSerializer, ILogger logger)
    {
        _secureJsonSerializer = secureJsonSerializer ?? throw new ArgumentNullException(nameof(secureJsonSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = new LocalBlobEntityMapper();
        _entityJsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>
    /// Saves entities to their respective files in the specified directory.
    /// </summary>
    public async Task SaveEntitiesAsync<T>(
        IEnumerable<T> entities,
        DirectoryInfo baseDirectory,
        string folderName,
        Func<T, string> fileNameSelector) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(fileNameSelector);

        var folderPath = new DirectoryInfo(Path.Combine(baseDirectory.FullName, folderName));
        if (!Directory.Exists(folderPath.FullName))
        {
            _logger.LogDebug("Folder {FolderName} does not exist in {BaseDirectory}, skipping",
                folderName, baseDirectory.FullName);
            return;
        }

        foreach (var entity in entities)
        {
            var fileName = fileNameSelector(entity);
            var filePath = Path.Combine(folderPath.FullName, fileName);

            if (File.Exists(filePath))
            {
                await SaveEntityFileAsync(entity, filePath);
            }
        }
    }

    /// <summary>
    /// Loads entities from their respective files in the specified directory.
    /// </summary>
    public async Task LoadEntitiesAsync<T>(
        IEnumerable<T> entities,
        DirectoryInfo baseDirectory,
        string folderName,
        Func<T, string> fileNameSelector,
        Func<T, string, Task> entityLoader) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(fileNameSelector);
        ArgumentNullException.ThrowIfNull(entityLoader);

        var folderPath = new DirectoryInfo(Path.Combine(baseDirectory.FullName, folderName));
        if (!Directory.Exists(folderPath.FullName))
        {
            _logger.LogDebug("Folder {FolderName} does not exist in {BaseDirectory}, skipping entity loading",
                folderName, baseDirectory.FullName);
            return;
        }

        var loadTasks = entities.Select(async entity =>
        {
            var fileName = fileNameSelector(entity);
            var filePath = Path.Combine(folderPath.FullName, fileName);
            await LoadEntityFileAsync(filePath, async rawJson => await entityLoader(entity, rawJson));
        });

        await Task.WhenAll(loadTasks);
    }

    private async Task SaveEntityFileAsync<T>(T entity, string filePath) where T : class
    {
        try
        {
            // No embeddings at this layer yet; pass null so mapper returns raw entity
            var persisted = _mapper.ToPersistedEntity(entity, null);
            var json = await _secureJsonSerializer.SerializeAsync(persisted, _entityJsonOptions);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            _logger.LogDebug("Successfully saved entity to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save entity to file: {FilePath}", filePath);
            throw;
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
    /// Creates a loader function for a specific entity type.
    /// </summary>
    public static Func<T, string, Task> CreateEntityLoader<T>() where T : ISemanticModelEntity
    {
        return async (entity, rawJson) =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var tempFile = Path.Combine(tempDir, $"{entity.Schema}.{entity.Name}.json");
                var contentJson = ExtractEnvelopeDataOrPassThrough(rawJson);
                await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
                await entity.LoadModelAsync(new DirectoryInfo(tempDir));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        };
    }

    /// <summary>
    /// Creates a file name selector for semantic model entities.
    /// </summary>
    public static Func<T, string> CreateEntityFileNameSelector<T>() where T : ISemanticModelEntity
    {
        return entity => string.Format(LocalDiskPersistenceConstants.FilePatterns.EntityFile,
            entity.Schema, entity.Name);
    }

    private static string ExtractEnvelopeDataOrPassThrough(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Check if this is a versioned envelope format (has both "version" and "data" properties)
                bool hasVersion = false;
                bool hasData = false;
                JsonElement dataElement = default;

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "version", StringComparison.OrdinalIgnoreCase))
                    {
                        hasVersion = true;
                    }
                    else if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                    {
                        hasData = true;
                        dataElement = prop.Value;
                    }
                }

                // If both version and data are present, this is the new envelope format
                if (hasVersion && hasData)
                {
                    return dataElement.GetRawText();
                }

                // Fallback: look for just "data" property (legacy envelope without version)
                if (hasData)
                {
                    return dataElement.GetRawText();
                }
            }
        }
        catch
        {
            // Fallback to pass-through on any parse error
        }

        // If no envelope detected, pass through as-is (legacy direct entity format)
        return jsonContent;
    }
}