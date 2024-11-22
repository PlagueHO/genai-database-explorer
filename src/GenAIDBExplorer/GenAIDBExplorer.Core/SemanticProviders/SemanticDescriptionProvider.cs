using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.ProjectLogger;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Resources;
using System.Text.Json;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Generates semantic descriptions for semantic model entities using Semantic Kernel.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public class SemanticDescriptionProvider(
        IProject project,
        ISemanticKernelFactory semanticKernelFactory,
        ISchemaRepository schemaRepository,
        ILogger<SemanticDescriptionProvider> logger,
        IProjectLoggerProvider projectLoggerProvider
    ) : ISemanticDescriptionProvider
{
    private readonly IProject _project = project;
    private readonly ISemanticKernelFactory _semanticKernelFactory = semanticKernelFactory;
    private readonly ISchemaRepository _schemaRepository = schemaRepository;
    private readonly ILogger<SemanticDescriptionProvider> _logger = logger;
    private readonly IProjectLoggerProvider _projectLoggerProvider = projectLoggerProvider;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SemanticDescriptionProvider).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SemanticDescriptionProvider).Assembly);

    private const string _promptyFolder = "Prompty";

    /// <summary>
    /// Generates semantic descriptions for the specified list of tables using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="tables"></param>
    /// <returns></returns>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, List<TableInfo> tables)
    {
        foreach (var table in tables)
        {
            var semanticModelTable = semanticModel.Tables.FirstOrDefault(t => t.Schema == table.SchemaName && t.Name == table.TableName);
            if (semanticModelTable != null && string.IsNullOrEmpty(semanticModelTable.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("TableMissingSemanticDescription"), table.SchemaName, table.TableName);
                await UpdateSemanticDescriptionAsync(semanticModel, semanticModelTable);
            }
        }
    }

    /// <summary>
    /// Generates a semantic description for the specified table using Semantic Kernel.
    /// </summary>
    /// <param name="table">The semantic model table for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelTable table)
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForTable"), table.Schema, table.Name);

        var promptyFilename = "semantic_model_describe_table.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Retrieve sample data for the table
        var sampleData = await _schemaRepository.GetSampleTableDataAsync(new TableInfo(table.Schema, table.Name));
        var sampleDataSerialized = SerializeSampleData(sampleData);

        var projectInfo = new
        {
            description = _project.Settings.Database.Description
        };
        var tableInfo = new
        {
            structure = table.ToYaml(),
            data = sampleDataSerialized
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

        table.SemanticDescription = result.ToString();
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForTable"), table.Schema, table.Name);
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of views using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="views"></param>
    /// <returns></returns>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, List<ViewInfo> views)
    {
        foreach (var view in views)
        {
            var semanticModelView = semanticModel.Views.FirstOrDefault(v => v.Schema == view.SchemaName && v.Name == view.ViewName);
            if (semanticModelView != null && string.IsNullOrEmpty(semanticModelView.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("ViewMissingSemanticDescription"), view.SchemaName, view.ViewName);
                await UpdateSemanticDescriptionAsync(semanticModel, semanticModelView);
            }
        }
    }

    /// <summary>
    /// Generates a semantic description for the specified view using Semantic Kernel.
    /// </summary>
    /// <param name="view">The semantic model view for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelView view)
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForView"), view.Schema, view.Name);

        // First get the list of tables used in the view definition
        var tableList = await GetTableListFromViewDefinitionAsync(semanticModel, view);

        // TODO: Refactor this into a separate method that can be used to update the semantic description for all tables in the model
        // For each table, find the table in the Semantic Model and check if it has a semantic description
        foreach (var table in tableList)
        {
            var semanticModelTable = semanticModel.Tables.FirstOrDefault(t => t.Schema == table.SchemaName && t.Name == table.TableName);
            if (semanticModelTable != null && string.IsNullOrEmpty(semanticModelTable.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("TableMissingSemanticDescription"), table.SchemaName, table.TableName);
                await UpdateSemanticDescriptionAsync(semanticModel, semanticModelTable);
            }
        }

        var promptyFilename = "semantic_model_describe_view.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Retrieve sample data for the view
        var sampleData = await _schemaRepository.GetSampleViewDataAsync(new ViewInfo(view.Schema, view.Name));
        var sampleDataSerialized = SerializeSampleData(sampleData);

        var projectInfo = new
        {
            description = _project.Settings.Database.Description
        };
        var viewInfo = new
        {
            structure = view.ToYaml(),
            data = sampleDataSerialized
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

        view.SemanticDescription = result.ToString();
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForView"), view.Schema, view.Name);
    }

    /// <summary>
    /// Generates a semantic description for the specified stored procedure using Semantic Kernel.
    /// </summary>
    /// <param name="storedProcedure">The semantic model stored procedure for which to generate the description.</param>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure)
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForStoredProcedure"), storedProcedure.Schema, storedProcedure.Name);

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

        storedProcedure.SemanticDescription = result.ToString();
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForStoredProcedure"), storedProcedure.Schema, storedProcedure.Name);
    }


    /// <summary>
    /// Generates semantic descriptions for the specified list of stored procedures using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="storedProcedures"></param>
    /// <returns></returns>
    public async Task UpdateSemanticDescriptionAsync(SemanticModel semanticModel, List<StoredProcedureInfo> storedProcedures)
    {
        foreach (var storedProcedure in storedProcedures)
        {
            var semanticModelStoredProcedure = semanticModel.StoredProcedures.FirstOrDefault(sp => sp.Schema == storedProcedure.SchemaName && sp.Name == storedProcedure.ProcedureName);
            if (semanticModelStoredProcedure != null && string.IsNullOrEmpty(semanticModelStoredProcedure.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("StoredProcedureMissingSemanticDescription"), storedProcedure.SchemaName, storedProcedure.ProcedureName);
                await UpdateSemanticDescriptionAsync(semanticModel, semanticModelStoredProcedure);
            }
        }
    }

    /// <summary>
    /// Gets a list of tables from the specified view definition using Semantic Kernel.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public async Task<List<TableInfo>> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view)
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GetTableListFromViewDefinition"), view.Schema, view.Name);
        var promptyFilename = "get_tables_from_view_definition.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        var viewInfo = new
        {
            definition = view.Definition
        };
        var arguments = new KernelArguments()
        {
            { "view", viewInfo }
        };

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // Invoke the semantic kernel function to get the list of tables from the view definition
        // and then try to serialize the result. If the JSON deserialization fails, log the error and retry
        // the prompty call up to 3 times.
        List<TableInfo> tableList = [];
        for (int i = 0; i < 3; i++)
        {
            var result = await semanticKernel.InvokeAsync(function, arguments);
            var resultString = result?.ToString();

            try
            {
                if (!string.IsNullOrEmpty(resultString))
                {
                    tableList = JsonSerializer.Deserialize<List<TableInfo>>(resultString);
                }
                break;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, _resourceManagerErrorMessages.GetString("ErrorDeserializingTableListFromViewDefinition"), view.Schema, view.Name);
                _logger.LogInformation(resultString);
            }
        }

        _logger.LogInformation(_resourceManagerLogMessages.GetString("GotTableListFromViewDefinition"), tableList.Count, view.Schema, view.Name);

        return tableList;
    }

    /// <summary>
    /// Serializes sample data.
    /// </summary>
    /// <param name="sampleData">The sample data to serialize.</param>
    /// <returns>The serialized data.</returns>
    private static string SerializeSampleData(List<Dictionary<string, object>> sampleData)
    {
        if (sampleData.Count > 0)
        {
            return JsonSerializer.Serialize(sampleData);
        }
        return "No sample data available";
    }
}