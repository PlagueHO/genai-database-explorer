using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Orchestrates the end-to-end process of generating semantic vectors for a semantic model.
/// </summary>
public sealed class VectorOrchestrator : IVectorOrchestrator
{
    private readonly IVectorGenerationService _generationService;
    private readonly ILogger<VectorOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorOrchestrator"/> class.
    /// </summary>
    /// <param name="generationService">The vector generation service.</param>
    /// <param name="logger">The logger instance.</param>
    public VectorOrchestrator(IVectorGenerationService generationService, ILogger<VectorOrchestrator> logger)
    {
        _generationService = generationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vector generation orchestration");
        return _generationService.GenerateAsync(model, projectPath, options, cancellationToken);
    }
}
