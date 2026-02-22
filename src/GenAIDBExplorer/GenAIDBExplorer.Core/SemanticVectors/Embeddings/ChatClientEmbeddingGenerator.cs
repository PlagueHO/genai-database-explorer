using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Repository.Performance;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.SemanticVectors.Embeddings;

/// <summary>
/// Generates embeddings using <see cref="IChatClientFactory"/> to obtain an
/// <see cref="IEmbeddingGenerator{String, Embedding}"/> instance.
/// Replaces <see cref="SemanticKernelEmbeddingGenerator"/> as part of the
/// Microsoft.Extensions.AI migration.
/// </summary>
public sealed class ChatClientEmbeddingGenerator(
    IChatClientFactory chatClientFactory,
    ILogger<ChatClientEmbeddingGenerator> logger,
    IPerformanceMonitor performanceMonitor
) : IEmbeddingGenerator
{
    private readonly IChatClientFactory _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
    private readonly ILogger<ChatClientEmbeddingGenerator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPerformanceMonitor _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<float>> GenerateAsync(string text, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must be provided", nameof(text));
        ArgumentNullException.ThrowIfNull(infrastructure);

        using var _ = _logger.BeginScope("EmbeddingGeneration:ServiceId={ServiceId}", infrastructure.EmbeddingServiceId ?? "default");

        using var perf = _performanceMonitor.StartOperation("Vector.Embeddings.Generate", new Dictionary<string, object>
        {
            ["ServiceId"] = infrastructure.EmbeddingServiceId ?? "default",
            ["Provider"] = infrastructure.Provider,
        });

        var generator = _chatClientFactory.CreateEmbeddingGenerator();

        var results = await generator.GenerateAsync([text], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (results is not null && results.Count > 0)
        {
            return results[0].Vector;
        }

        _logger.LogWarning("Embedding generation returned no result");
        perf.MarkAsFailed("No embedding result returned");
        return ReadOnlyMemory<float>.Empty;
    }
}
