using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using GenAIDBExplorer.AI.SemanticKernel;
using GenAIDBExplorer.Models.SemanticModel;
using GenAIDBExplorer.Data.SemanticModelProviders;
using GenAIDBExplorer.Models.Project;
using System.Text.Json;


namespace GenAIDBExplorer.AI.SemanticProviders;

/// <summary>
/// Generates semantic descriptions for semantic model entities using Semantic Kernel.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public class SemanticDescriptionProvider(
        IProject project,
        ISemanticModelProvider semanticModelProvider,
        ISemanticKernelFactory semanticKernelFactory,
        ISchemaRepository schemaRespository,
        ILogger<SemanticDescriptionProvider> logger
    ) : ISemanticDescriptionProvider
{
    private readonly IProject _project = project;
    private readonly ISemanticModelProvider _semanticModelProvider = semanticModelProvider;
    private readonly ISemanticKernelFactory _semanticKernelFactory = semanticKernelFactory;
    private readonly ISchemaRepository _schemaRepository = schemaRespository;
    private readonly ILogger<SemanticDescriptionProvider> _logger = logger;

    private const string _promptyFolder = "Prompty";

    public async Task UpdateSemanticDescriptionAsync(SemanticModelTable table)
    {
        _logger.LogInformation("Generating semantic description for table {Schema}.{Name}", table.Schema, table.Name);

        var promptyFilename = "semantic_model_describe_table.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        var sampleData = await _schemaRepository.GetSampleTableDataAsync(new TableInfo(table.Schema, table.Name));
        var sampleDataJson = "No sample data available";
        // Serialize sample data to JSON
        if (sampleData.Count > 0)
        {
            sampleDataJson = JsonSerializer.Serialize(sampleData);
        }

        var projectInfo = new
        {
            description = _project.Settings.Database.Description
        };
        var tableInfo = new
        {
            structure = table.ToYaml(),
            data = sampleDataJson
        };

        var arguments = new KernelArguments()
        {
            { "table", tableInfo },
            { "project", projectInfo }
        };

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var result = await semanticKernel.InvokeAsync(function, arguments);

        _logger.LogInformation("Completed generation of semantic description for table {Schema}.{Name}", table.Schema, table.Name);
        table.SemanticDescription = result.ToString();
    }
}