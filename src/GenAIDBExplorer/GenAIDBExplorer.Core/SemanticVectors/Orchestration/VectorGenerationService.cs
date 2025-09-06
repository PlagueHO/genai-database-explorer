using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Keys;
using GenAIDBExplorer.Core.SemanticVectors.Mapping;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Core.Repository.Performance;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Service for generating semantic vector embeddings for a semantic model.
/// </summary>
public sealed class VectorGenerationService : IVectorGenerationService
{
    private readonly ProjectSettings _projectSettings;
    private readonly IVectorInfrastructureFactory _infrastructureFactory;
    private readonly IVectorRecordMapper _recordMapper;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IEntityKeyBuilder _keyBuilder;
    private readonly IVectorIndexWriter _indexWriter;
    private readonly ISecureJsonSerializer _secureJsonSerializer;
    private readonly ISemanticModelRepository _repository;
    private readonly ILogger<VectorGenerationService> _logger;
    private readonly IPerformanceMonitor _performanceMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorGenerationService"/> class.
    /// </summary>
    /// <param name="projectSettings">The project settings.</param>
    /// <param name="infrastructureFactory">The vector infrastructure factory.</param>
    /// <param name="recordMapper">The record mapper.</param>
    /// <param name="embeddingGenerator">The embedding generator.</param>
    /// <param name="keyBuilder">The entity key builder.</param>
    /// <param name="indexWriter">The vector index writer.</param>
    /// <param name="secureJsonSerializer">The secure JSON serializer.</param>
    /// <param name="repository">The semantic model repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="performanceMonitor">The performance monitor.</param>
    public VectorGenerationService(
        ProjectSettings projectSettings,
        IVectorInfrastructureFactory infrastructureFactory,
        IVectorRecordMapper recordMapper,
        IEmbeddingGenerator embeddingGenerator,
        IEntityKeyBuilder keyBuilder,
        IVectorIndexWriter indexWriter,
        ISecureJsonSerializer secureJsonSerializer,
        ISemanticModelRepository repository,
        ILogger<VectorGenerationService> logger,
        IPerformanceMonitor performanceMonitor
    )
    {
        _projectSettings = projectSettings;
        _infrastructureFactory = infrastructureFactory;
        _recordMapper = recordMapper;
        _embeddingGenerator = embeddingGenerator;
        _keyBuilder = keyBuilder;
        _indexWriter = indexWriter;
        _secureJsonSerializer = secureJsonSerializer;
        _repository = repository;
        _logger = logger;
        _performanceMonitor = performanceMonitor;
    }

    /// <inheritdoc/>
    public async Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(projectPath);
        // ...existing code...
        using var perfAll = _performanceMonitor.StartOperation("Vector.Generate.All", new Dictionary<string, object>
        {
            ["ModelName"] = model.Name
        });

        var repoStrategy = _projectSettings.SemanticModel.PersistenceStrategy;
        var infra = _infrastructureFactory.Create(_projectSettings.VectorIndex, repoStrategy);
        var processed = 0;

        // Select entities
        var tables = options.SkipTables ? [] : (await model.GetTablesAsync()).ToList();
        var views = options.SkipViews ? [] : (await model.GetViewsAsync()).ToList();
        var sps = options.SkipStoredProcedures ? [] : (await model.GetStoredProceduresAsync()).ToList();

        // Filter by specific object if provided
        if (!string.IsNullOrWhiteSpace(options.ObjectType) && !string.IsNullOrWhiteSpace(options.SchemaName) && !string.IsNullOrWhiteSpace(options.ObjectName))
        {
            var type = options.ObjectType!.ToLowerInvariant();
            tables = type == "table" ? tables.Where(t => t.Schema == options.SchemaName && t.Name == options.ObjectName).ToList() : [];
            views = type == "view" ? views.Where(v => v.Schema == options.SchemaName && v.Name == options.ObjectName).ToList() : [];
            sps = type == "storedprocedure" ? sps.Where(sp => sp.Schema == options.SchemaName && sp.Name == options.ObjectName).ToList() : [];
        }

        async Task ProcessEntityAsync(SemanticModelEntity entity, DirectoryInfo modelRoot)
        {
            using var perf = _performanceMonitor.StartOperation("Vector.Generate.Entity", new Dictionary<string, object>
            {
                ["Schema"] = entity.Schema,
                ["Name"] = entity.Name,
                ["Type"] = entity.GetType().Name
            });
            // ...existing code...
            var content = _recordMapper.BuildEntityText(entity);
            var contentHash = _keyBuilder.BuildContentHash(content);
            var id = _keyBuilder.BuildKey(model.Name, entity.GetType().Name, entity.Schema, entity.Name);

            // Determine entity type string for repository lookup
            var entityType = entity switch
            {
                SemanticModelTable => "tables",
                SemanticModelView => "views",
                SemanticModelStoredProcedure => "storedprocedures",
                _ => null
            };
            if (entityType is null) return;

            // Use repository pattern for persistence-strategy-aware vector existence checking
            var existingHash = await _repository.CheckVectorExistsAsync(
                entityType, entity.Schema, entity.Name, contentHash, projectPath,
                _projectSettings.SemanticModel?.PersistenceStrategy, cancellationToken).ConfigureAwait(false);

            if (!options.Overwrite && string.Equals(existingHash?.Trim(), contentHash?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping unchanged entity {Schema}.{Name}", entity.Schema, entity.Name);
                return;
            }

            // If overwrite is requested and content hash matches, we still need to update the file timestamp
            // to indicate the overwrite operation occurred, even if we don't regenerate embeddings
            if (options.Overwrite && string.Equals(existingHash?.Trim(), contentHash?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Overwriting unchanged entity {Schema}.{Name}", entity.Schema, entity.Name);

                // TODO: With repository pattern, timestamp updates should be handled by the persistence strategy
                processed++;
                return;
            }

            if (options.DryRun)
            {
                _logger.LogInformation("[DryRun] Would generate embedding for {Schema}.{Name}", entity.Schema, entity.Name);
                processed++;
                return;
            }

            // Generate embedding
            var vector = await _embeddingGenerator.GenerateAsync(content, infra, cancellationToken).ConfigureAwait(false);
            if (vector.IsEmpty)
            {
                _logger.LogWarning("Embedding generation returned empty vector for {Schema}.{Name}", entity.Schema, entity.Name);
                perf.MarkAsFailed("Empty vector");
                // TODO: With repository pattern, timestamp updates should be handled by the persistence strategy
                return;
            }

            // Persist envelope for Local/Blob strategies per Phase 2
            if (string.Equals(repoStrategy, "LocalDisk", StringComparison.OrdinalIgnoreCase) || string.Equals(repoStrategy, "AzureBlob", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: With repository pattern, vector persistence should be handled by the repository
                // Currently keeping the original LocalDisk file writing logic for backward compatibility
                // This should be refactored to use repository.SaveVectorAsync() or similar method
                var mapper = new LocalBlobEntityMapper();
                var payload = new EmbeddingPayload
                {
                    Vector = vector.ToArray(),
                    Metadata = new EmbeddingMetadata
                    {
                        ModelId = infra.Settings.EmbeddingServiceId,
                        Dimensions = vector.Length,
                        ContentHash = contentHash,
                        GeneratedAt = DateTimeOffset.UtcNow,
                        ServiceId = infra.EmbeddingServiceId,
                        Version = "1"
                    }
                };
                var envelope = mapper.ToPersistedEntity(entity, payload);
                var json = await _secureJsonSerializer.SerializeAsync(envelope, new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);

                // For now, recreate the file path logic until we have full repository integration
                var folderName = entityType;
                var fileName = $"{entity.Schema}.{entity.Name}.json";
                var tempModelRoot = new DirectoryInfo(Path.Combine(projectPath.FullName, _projectSettings.SemanticModelRepository?.LocalDisk?.Directory ?? "semantic-model"));
                var entityDir = new DirectoryInfo(Path.Combine(tempModelRoot.FullName, folderName));
                Directory.CreateDirectory(entityDir.FullName);
                var entityFile = new FileInfo(Path.Combine(entityDir.FullName, fileName));

                await File.WriteAllTextAsync(entityFile.FullName, json, cancellationToken).ConfigureAwait(false);

                // Explicitly update timestamp after file write to ensure overwrite is reflected in filesystem
                if (options.Overwrite)
                {
                    try
                    {
                        File.SetLastWriteTimeUtc(entityFile.FullName, DateTime.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to update envelope timestamp for {Schema}.{Name}", entity.Schema, entity.Name);
                    }
                }
            }

            // Upsert into vector index
            var record = _recordMapper.ToRecord(entity, id, content, vector, contentHash ?? string.Empty);
            await _indexWriter.UpsertAsync(record, infra, cancellationToken).ConfigureAwait(false);

            processed++;
        }

        // Resolve model directory (LocalDisk only for write path)
        // For LocalDisk, projectSettings.SemanticModelRepository.LocalDisk.Directory is relative to project path
        var modelDir = new DirectoryInfo(Path.Combine(projectPath.FullName, _projectSettings.SemanticModelRepository?.LocalDisk?.Directory ?? "semantic-model"));
        Directory.CreateDirectory(modelDir.FullName);

        foreach (var t in tables) { await ProcessEntityAsync(t, modelDir); }
        foreach (var v in views) { await ProcessEntityAsync(v, modelDir); }
        foreach (var sp in sps) { await ProcessEntityAsync(sp, modelDir); }

        return processed;
    }

    // ...existing code...

    private static async Task<string> ReadAllTextRobustAsync(string path, CancellationToken cancellationToken)
    {
        // Try multiple strategies to avoid transient file locks on Windows
        const int maxAttempts = 5;
        int attempt = 0;
        for (; ; )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Shared read stream to reduce sharing violations
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, useAsync: true);
                using var sr = new StreamReader(fs);
                return await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException) when (++attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) when (attempt++ < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object) return false;
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        return false;
    }

    private static bool FindStringPropertyRecursive(JsonElement element, string name, out string? value)
    {
        value = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            value = prop.Value.GetString();
                            return true;
                        }
                    }
                    if (FindStringPropertyRecursive(prop.Value, name, out value)) return true;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (FindStringPropertyRecursive(item, name, out value)) return true;
                }
                break;
        }
        return false;
    }
}
