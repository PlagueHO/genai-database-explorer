using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

public sealed class VectorOrchestrator(IVectorGenerationService generationService, ILogger<VectorOrchestrator> logger) : IVectorOrchestrator
{
    private readonly IVectorGenerationService _generationService = generationService;
    private readonly ILogger<VectorOrchestrator> _logger = logger;

    public Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vector generation orchestration");
        return _generationService.GenerateAsync(model, projectPath, options, cancellationToken);
    }
}
