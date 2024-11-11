using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using GenAIDBExplorer.AI.SemanticKernel;
using GenAIDBExplorer.Models.SemanticModel;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.AI.SemanticProviders;

/// <summary>
/// Generates semantic descriptions for semantic model entities using Semantic Kernel.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public class SemanticDescriptionProvider(
        IProject project,
        ISemanticModelProvider semanticModelProvider,
        ISemanticKernelFactory semanticKernelFactory,
        ILogger<SemanticDescriptionProvider> logger
    ) : ISemanticDescriptionProvider
{
    private readonly IProject _project = project;
    private readonly ISemanticModelProvider _semanticModelProvider = semanticModelProvider;
    private readonly ISemanticKernelFactory _semanticKernelFactory = semanticKernelFactory;
    private readonly ILogger<SemanticDescriptionProvider> _logger = logger;

    public async Task UpdateSemanticDescriptionAsync(SemanticModelTable table)
    {
        _logger.LogInformation("Generating semantic description for table {Schema}.{Name}", table.Schema, table.Name);
        var prompt = @"You are a generative AI SQL database assistant. Your goal is to clearly describe the puropose of the SQL database table with the following properties: Name {table.Schema}.{table.Name}";

        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();
        var result = await semanticKernel.InvokePromptAsync(prompt);

        _logger.LogInformation("Completed generation of semantic description for table {Schema}.{Name}", table.Schema, table.Name);
        table.SemanticDescription = result.ToString();
    }
}