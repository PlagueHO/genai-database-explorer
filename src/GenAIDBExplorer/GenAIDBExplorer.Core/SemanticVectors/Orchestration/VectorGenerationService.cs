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

            // Determine file path based on entity type
            var folderName = entity switch
            {
                SemanticModelTable => "tables",
                SemanticModelView => "views",
                SemanticModelStoredProcedure => "storedprocedures",
                _ => null
            };
            if (folderName is null) return;

            var entityDir = new DirectoryInfo(Path.Combine(modelRoot.FullName, folderName));
            Directory.CreateDirectory(entityDir.FullName);
            var fileName = $"{entity.Schema}.{entity.Name}.json";
            var entityFile = new FileInfo(Path.Combine(entityDir.FullName, fileName));

            // If the expected file isn't at the configured root, probe common alternate roots
            // to ensure we detect prior persisted envelopes regardless of directory casing.
            if (!entityFile.Exists)
            {
                var alt1 = new FileInfo(Path.Combine(projectPath.FullName, "semantic-model", folderName, fileName));
                var alt2 = new FileInfo(Path.Combine(projectPath.FullName, "SemanticModel", folderName, fileName));
                if (alt1.Exists) entityFile = alt1;
                else if (alt2.Exists) entityFile = alt2;
            }

            // Read existing envelope if present to check metadata hash
            string? existingHash = null;
            if (entityFile.Exists)
            {
                try
                {
                    // Use a robust read to avoid transient sharing violations on Windows (e.g., AV/indexers)
                    var raw = await ReadAllTextRobustAsync(entityFile.FullName, cancellationToken).ConfigureAwait(false);

                    // Ultra-fast path: if not overwriting and the raw JSON already contains the same contentHash,
                    // short-circuit and skip without any further parsing. This avoids any flakiness due to
                    // parser differences and guarantees idempotent behavior.
                    if (!options.Overwrite && !string.IsNullOrEmpty(contentHash))
                    {
                        if (raw.IndexOf("\"contentHash\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            raw.IndexOf(contentHash, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _logger.LogInformation("Skipping unchanged entity {Schema}.{Name}", entity.Schema, entity.Name);
                            return;
                        }
                    }

                    // Primary: fast regex capture for contentHash value regardless of object shape
                    var capture = Regex.Match(raw, "\"contentHash\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (capture.Success)
                    {
                        existingHash = capture.Groups[1].Value;
                    }

                    // Secondary: deserialize using secure serializer and inspect typed DTO
                    if (string.IsNullOrEmpty(existingHash))
                    {
                        try
                        {
                            var envelope = await _secureJsonSerializer.DeserializeAsync<PersistedEntityDto>(raw).ConfigureAwait(false);
                            existingHash = envelope?.Embedding?.Metadata?.ContentHash;
                        }
                        catch
                        {
                            // Ignore and fall back to manual parsing
                        }
                    }

                    if (string.IsNullOrEmpty(existingHash))
                    {
                        using var doc = JsonDocument.Parse(raw);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            // Primary path: embedding -> metadata -> contentHash (case-insensitive)
                            if (TryGetPropertyIgnoreCase(doc.RootElement, "embedding", out var emb) &&
                                TryGetPropertyIgnoreCase(emb, "metadata", out var md) &&
                                TryGetPropertyIgnoreCase(md, "contentHash", out var ch))
                            {
                                existingHash = ch.GetString();
                            }
                            // Fallback path: metadata -> contentHash at root (case-insensitive)
                            else if (TryGetPropertyIgnoreCase(doc.RootElement, "metadata", out var md2) &&
                                     TryGetPropertyIgnoreCase(md2, "contentHash", out var ch2))
                            {
                                existingHash = ch2.GetString();
                            }
                            // Recursive fallback: find any property named contentHash
                            else if (FindStringPropertyRecursive(doc.RootElement, "contentHash", out var foundHash))
                            {
                                existingHash = foundHash;
                            }
                            // Last resort: if the raw contains the newly computed hash anywhere, assume match
                            else if (!string.IsNullOrEmpty(contentHash) && raw.IndexOf(contentHash, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                existingHash = contentHash;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore corrupted envelope; treat as overwrite
                }
            }

            if (!options.Overwrite && string.Equals(existingHash?.Trim(), contentHash?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping unchanged entity {Schema}.{Name}", entity.Schema, entity.Name);
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
                return;
            }

            // Persist envelope for Local/Blob strategies per Phase 2
            if (string.Equals(repoStrategy, "LocalDisk", StringComparison.OrdinalIgnoreCase) || string.Equals(repoStrategy, "AzureBlob", StringComparison.OrdinalIgnoreCase))
            {
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
                await File.WriteAllTextAsync(entityFile.FullName, json, cancellationToken).ConfigureAwait(false);
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
