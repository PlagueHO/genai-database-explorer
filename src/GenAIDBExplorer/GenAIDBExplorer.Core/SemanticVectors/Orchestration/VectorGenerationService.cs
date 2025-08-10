using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

public sealed class VectorGenerationService(
    ProjectSettings projectSettings,
    IVectorInfrastructureFactory infrastructureFactory,
    IVectorRecordMapper recordMapper,
    IEmbeddingGenerator embeddingGenerator,
    IEntityKeyBuilder keyBuilder,
    IVectorIndexWriter indexWriter,
    ISecureJsonSerializer secureJsonSerializer,
    ILogger<VectorGenerationService> logger
) : IVectorGenerationService
{
    private readonly ProjectSettings _projectSettings = projectSettings;
    private readonly IVectorInfrastructureFactory _infrastructureFactory = infrastructureFactory;
    private readonly IVectorRecordMapper _recordMapper = recordMapper;
    private readonly IEmbeddingGenerator _embeddingGenerator = embeddingGenerator;
    private readonly IEntityKeyBuilder _keyBuilder = keyBuilder;
    private readonly IVectorIndexWriter _indexWriter = indexWriter;
    private readonly ISecureJsonSerializer _secureJsonSerializer = secureJsonSerializer;
    private readonly ILogger<VectorGenerationService> _logger = logger;

    public async Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(projectPath);

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
            // Build content and hash
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
            var entityFile = new FileInfo(Path.Combine(entityDir.FullName, $"{entity.Schema}.{entity.Name}.json"));

            // Read existing envelope if present to check metadata hash
            string? existingHash = null;
            if (entityFile.Exists)
            {
                try
                {
                    var raw = await File.ReadAllTextAsync(entityFile.FullName, cancellationToken).ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("embedding", out var emb) &&
                        emb.TryGetProperty("metadata", out var md) &&
                        md.TryGetProperty("contentHash", out var ch))
                    {
                        existingHash = ch.GetString();
                    }
                }
                catch
                {
                    // ignore corrupted envelope; treat as overwrite
                }
            }

            if (!options.Overwrite && string.Equals(existingHash, contentHash, StringComparison.Ordinal))
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
            var record = _recordMapper.ToRecord(entity, id, content, vector, contentHash);
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
}
