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
using Microsoft.SemanticKernel.Connectors.OpenAI;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing.Charts;

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
    /// Generates semantic descriptions for all tables using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public async Task UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        await Parallel.ForEachAsync(semanticModel.Tables, GetParallelismOptions(), async (table, cancellationToken) =>
        {
            await UpdateTableSemanticDescriptionAsync(semanticModel, table).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of tables using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="tables"></param>
    /// <returns></returns>
    public async Task UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, TableList tables)
    {
        await Parallel.ForEachAsync(tables.Tables, GetParallelismOptions(), async (table, cancellationToken) =>
        {
            var semanticModelTable = semanticModel.Tables.FirstOrDefault(t => t.Schema == table.SchemaName && t.Name == table.TableName);
            if (semanticModelTable != null && string.IsNullOrEmpty(semanticModelTable.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("TableMissingSemanticDescription"), table.SchemaName, table.TableName);
                await UpdateTableSemanticDescriptionAsync(semanticModel, semanticModelTable).ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Generates a semantic description for the specified table using Semantic Kernel.
    /// </summary>
    /// <param name="table">The semantic model table for which to generate the description.</param>
    /// <returns></returns>
    public async Task UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelTable table)
    {
        using (_logger.BeginScope("Table [{Schema}.{Name}]", table.Name, table.Schema))
        {
            _logger.LogInformation(_resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForTable"), table.Schema, table.Name);

            // Retrieve sample data for the table
            var sampleData = await _schemaRepository.GetSampleTableDataAsync(
                new TableInfo(table.Schema, table.Name)
            );
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

            var promptExecutionSettings = new PromptExecutionSettings
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ServiceId = "ChatCompletion"
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            };

            var arguments = new KernelArguments(promptExecutionSettings)
            {
                { "table", tableInfo },
                { "project", projectInfo }
            };

            var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();
            var promptyFilename = "semantic_model_describe_table.prompty";
            promptyFilename = Path.Combine(_promptyFolder, promptyFilename);

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Invoke the semantic kernel function to generate the description
            var result = await semanticKernel.InvokeAsync(function, arguments);

            table.SemanticDescription = result.ToString();
            _logger.LogInformation(_resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForTable"), table.Schema, table.Name);
        }
    }

    /// <summary>
    /// Generates semantic descriptions for all views using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public async Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        await Parallel.ForEachAsync(semanticModel.Views, GetParallelismOptions(), async (view, cancellationToken) =>
        {
            await UpdateViewSemanticDescriptionAsync(semanticModel, view).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of views using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="views"></param>
    /// <returns></returns>
    public async Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, ViewList views)
    {
        await Parallel.ForEachAsync(views.Views, GetParallelismOptions(), async (view, cancellationToken) =>
        {
            var semanticModelView = semanticModel.Views.FirstOrDefault(v => v.Schema == view.SchemaName && v.Name == view.ViewName);
            if (semanticModelView != null && string.IsNullOrEmpty(semanticModelView.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("ViewMissingSemanticDescription"), view.SchemaName, view.ViewName);
                await UpdateViewSemanticDescriptionAsync(semanticModel, semanticModelView).ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Generates a semantic description for the specified view using Semantic Kernel.
    /// </summary>
    /// <param name="view">The semantic model view for which to generate the description.</param>
    public async Task UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelView view)
    {
        using (_logger.BeginScope("View [{Schema}.{Name}]", view.Name, view.Schema))
        {
            _logger.LogInformation(_resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForView"), view.Schema, view.Name);

            // First get the list of tables used in the view definition
            var tableList = await GetTableListFromViewDefinitionAsync(semanticModel, view);

            // TODO: Refactor this into a separate method that can be used to update the semantic description for all tables in the model
            // For each table, find the table in the Semantic Model and check if it has a semantic description
            foreach (var table in tableList.Tables)
            {
                var semanticModelTable = semanticModel.Tables.FirstOrDefault(t => t.Schema == table.SchemaName && t.Name == table.TableName);
                if (semanticModelTable != null && string.IsNullOrEmpty(semanticModelTable.SemanticDescription))
                {
                    _logger.LogInformation(_resourceManagerLogMessages.GetString("TableMissingSemanticDescription"), table.SchemaName, table.TableName);
                    await UpdateTableSemanticDescriptionAsync(semanticModel, semanticModelTable);
                }
            }

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

            var promptExecutionSettings = new PromptExecutionSettings
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ServiceId = "ChatCompletion"
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            };

            var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "view", viewInfo },
            { "project", projectInfo }
        };

            var promptyFilename = "semantic_model_describe_view.prompty";
            promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
            var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Invoke the semantic kernel function to generate the description
            var result = await semanticKernel.InvokeAsync(function, arguments);

            view.SemanticDescription = result.ToString();
            _logger.LogInformation(_resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForView"), view.Schema, view.Name);
        }
    }

    /// <summary>
    /// Generates semantic descriptions for all stored procedures using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public async Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        await Parallel.ForEachAsync(semanticModel.StoredProcedures, GetParallelismOptions(), async (storedProcedure, cancellationToken) =>
        {
            await UpdateStoredProcedureSemanticDescriptionAsync(semanticModel, storedProcedure).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of stored procedures using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="storedProcedures"></param>
    /// <returns></returns>
    public async Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, StoredProcedureList storedProcedureList)
    {
        await Parallel.ForEachAsync(storedProcedureList.StoredProcedures, GetParallelismOptions(), async (storedProcedure, cancellationToken) =>
        {
            var semanticModelStoredProcedure = semanticModel.StoredProcedures.FirstOrDefault(sp => sp.Schema == storedProcedure.SchemaName && sp.Name == storedProcedure.ProcedureName);
            if (semanticModelStoredProcedure != null && string.IsNullOrEmpty(semanticModelStoredProcedure.SemanticDescription))
            {
                _logger.LogInformation(_resourceManagerLogMessages.GetString("StoredProcedureMissingSemanticDescription"), storedProcedure.SchemaName, storedProcedure.ProcedureName);
                await UpdateStoredProcedureSemanticDescriptionAsync(semanticModel, semanticModelStoredProcedure).ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Generates a semantic description for the specified stored procedure using Semantic Kernel.
    /// </summary>
    /// <param name="storedProcedure">The semantic model stored procedure for which to generate the description.</param>
    public async Task UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure)
    {
        using (_logger.BeginScope("Stored Procedure [{Schema}.{Name}]", storedProcedure.Name, storedProcedure.Schema))
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

            var promptExecutionSettings = new PromptExecutionSettings
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ServiceId = "ChatCompletion"
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            };

            var arguments = new KernelArguments(promptExecutionSettings)
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
    }

    /// <summary>
    /// Gets a list of tables from the specified view definition using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="view">The view to get the list of tables from</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of tables.</returns>
    public async Task<TableList> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view)
    {
        _logger.LogInformation(_resourceManagerLogMessages.GetString("GetTableListFromViewDefinition"), view.Schema, view.Name);

        var promptyFilename = "get_tables_from_view_definition.prompty";
        promptyFilename = Path.Combine(_promptyFolder, promptyFilename);
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ServiceId = "ChatCompletionStructured",
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ResponseFormat = typeof(TableList)
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        };

        var viewInfo = new
        {
            definition = view.Definition
        };
        var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "view", viewInfo }
        };

        var result = await semanticKernel.InvokeAsync(function, arguments);
        var resultString = result?.ToString();
        var tableList = new TableList();
        if (string.IsNullOrEmpty(resultString))
        {
            _logger.LogWarning(_resourceManagerLogMessages.GetString("SemanticKernelReturnedEmptyResult"));
        }
        else
        {
            tableList = JsonSerializer.Deserialize<TableList>(resultString);
        }

        _logger.LogInformation(_resourceManagerLogMessages.GetString("GotTableListFromViewDefinition"), tableList.Tables.Count, view.Schema, view.Name);

        return tableList;
    }

    /// <summary>
    /// Gets a list of tables from the specified stored procedure using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="storedProcedure">The stored procedure to get the list of tables from</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of tables.</returns>
    public async Task<TableList> GetTableListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a list of views from the specified stored procedure using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="storedProcedure">The stored procedure to get the list of views from</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of views.</returns>
    public async Task<ViewList> GetViewListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure)
    {
        throw new NotImplementedException();
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

    // <summary>
    /// Gets the parallelism options semantic tasks on the semantic model.
    /// </summary>
    /// <returns>The parallelism options.</returns>
    private ParallelOptions GetParallelismOptions()
    {
        return new ParallelOptions
        {
            MaxDegreeOfParallelism = _project.Settings.SemanticModel.MaxDegreeOfParallelism
        };
    }
}