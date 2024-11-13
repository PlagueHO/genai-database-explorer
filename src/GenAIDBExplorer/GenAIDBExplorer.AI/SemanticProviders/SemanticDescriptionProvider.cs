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

    /// <summary>
    /// Generates a semantic description for the specified table using the Semantic Kernel.
    /// </summary>
    /// <param name="table">The semantic model table for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModelTable table)
    {
        _logger.LogInformation("Generating semantic description for table {Schema}.{Name}", table.Schema, table.Name);

        var promptyFilename = "semantic_model_describe_table.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Retrieve sample data for the table
        var sampleData = await _schemaRepository.GetSampleTableDataAsync(new TableInfo(table.Schema, table.Name));
        var sampleDataJson = "No sample data available";
        if (sampleData.Count > 0)
        {
            // Serialize sample data to JSON
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

        // Invoke the semantic kernel function to generate the description
        var result = await semanticKernel.InvokeAsync(function, arguments);

        _logger.LogInformation("Completed generation of semantic description for table {Schema}.{Name}", table.Schema, table.Name);
        table.SemanticDescription = result.ToString();
    }

    /// <summary>
    /// Generates a semantic description for the specified view using the Semantic Kernel.
    /// </summary>
    /// <param name="view">The semantic model view for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModelView view)
    {
        _logger.LogInformation("Generating semantic description for view {Schema}.{Name}", view.Schema, view.Name);

        var promptyFilename = "semantic_model_describe_view.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Retrieve sample data for the view
        var sampleData = await _schemaRepository.GetSampleViewDataAsync(new ViewInfo(view.Schema, view.Name));
        var sampleDataJson = "No sample data available";
        if (sampleData.Count > 0)
        {
            // Serialize sample data to JSON
            sampleDataJson = JsonSerializer.Serialize(sampleData);
        }

        var projectInfo = new
        {
            description = _project.Settings.Database.Description
        };
        var viewInfo = new
        {
            structure = view.ToYaml(),
            data = sampleDataJson
        };

        var arguments = new KernelArguments()
        {
            { "view", viewInfo },
            { "project", projectInfo }
        };

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // Invoke the semantic kernel function to generate the description
        var result = await semanticKernel.InvokeAsync(function, arguments);

        _logger.LogInformation("Completed generation of semantic description for view {Schema}.{Name}", view.Schema, view.Name);
        view.SemanticDescription = result.ToString();
    }

    /// <summary>
    /// Generates a semantic description for the specified stored procedure using the Semantic Kernel.
    /// </summary>
    /// <param name="storedProcedure">The semantic model stored procedure for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModelStoredProcedure storedProcedure)
    {
        _logger.LogInformation("Generating semantic description for stored procedure {Schema}.{Name}", storedProcedure.Schema, storedProcedure.Name);

        var promptyFilename = "semantic_model_describe_stored_procedure.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        var projectInfo = new
        {
            description = _project.Settings.Database.Description
        };
        var storedProcedureInfo = new
        {
            definition = storedProcedure.Definition,
            parameters = storedProcedure.Parameters
        };

        var arguments = new KernelArguments()
        {
            { "storedProcedure", storedProcedureInfo },
            { "project", projectInfo }
        };

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // Invoke the semantic kernel function to generate the description
        var result = await semanticKernel.InvokeAsync(function, arguments);

        _logger.LogInformation("Completed generation of semantic description for stored procedure {Schema}.{Name}", storedProcedure.Schema, storedProcedure.Name);
        storedProcedure.SemanticDescription = result.ToString();
    }
}