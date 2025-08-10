using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Embeddings;

namespace GenAIDBExplorer.Core.SemanticVectors.Embeddings;

public class SemanticKernelEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly ISemanticKernelFactory _semanticKernelFactory;
    private readonly ILogger<SemanticKernelEmbeddingGenerator> _logger;

    public SemanticKernelEmbeddingGenerator(ISemanticKernelFactory semanticKernelFactory, ILogger<SemanticKernelEmbeddingGenerator> logger)
    {
        _semanticKernelFactory = semanticKernelFactory ?? throw new ArgumentNullException(nameof(semanticKernelFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReadOnlyMemory<float>> GenerateAsync(string text, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must be provided", nameof(text));
        if (infrastructure is null) throw new ArgumentNullException(nameof(infrastructure));

        using var _ = _logger.BeginScope("EmbeddingGeneration:ServiceId={ServiceId}", infrastructure.EmbeddingServiceId ?? "default");

        var kernel = _semanticKernelFactory.CreateSemanticKernel();
        var serviceId = infrastructure.EmbeddingServiceId;

        // Try to resolve the new Microsoft.Extensions.AI embedding generator first
        IEmbeddingGenerator<string, Embedding<float>>? generator = null;
        try
        {
            generator = string.IsNullOrWhiteSpace(serviceId)
                ? kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
                : kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(serviceId!);
        }
        catch (Exception ex)
        {
            // Gracefully handle missing service registrations
            _logger.LogWarning(ex, "No embedding generator service was found in the kernel for ServiceId '{ServiceId}'", serviceId);
            return ReadOnlyMemory<float>.Empty;
        }

        // Use the simple overload that returns a list for a single input
        var results = await generator.GenerateAsync([text], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (results is not null && results.Count > 0)
        {
            return results[0].Vector;
        }

        _logger.LogWarning("Embedding generation returned no result");
        return ReadOnlyMemory<float>.Empty;
    }
}
