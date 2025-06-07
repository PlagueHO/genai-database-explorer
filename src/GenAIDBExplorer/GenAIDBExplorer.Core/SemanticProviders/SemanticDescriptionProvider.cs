using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Resources;
using System.Text.Json;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using GenAIDBExplorer.Core.Models.SemanticModel.Extensions;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Generates semantic descriptions for semantic model entities using Semantic Kernel.
/// </summary>
/// <typeparam name="TEntity">The type of semantic model entity.</typeparam>
public class SemanticDescriptionProvider(
        IProject project,
        ISemanticKernelFactory semanticKernelFactory,
        ISchemaRepository schemaRepository,
        ILogger<SemanticDescriptionProvider> logger
    ) : ISemanticDescriptionProvider
{
    private readonly IProject _project = project;
    private readonly ISemanticKernelFactory _semanticKernelFactory = semanticKernelFactory;
    private readonly ISchemaRepository _schemaRepository = schemaRepository;
    private readonly ILogger<SemanticDescriptionProvider> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SemanticDescriptionProvider).Assembly);

    private const string _promptyFolder = "Prompty";

    public async Task<SemanticProcessResult> UpdateSemanticDescriptionAsync<T>(
        SemanticModel semanticModel,
        IEnumerable<T> entities
    ) where T : SemanticModelEntity
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(entities, GetParallelismOptions(), async (entity, cancellationToken) =>
        {
            var result = await UpdateSemanticDescriptionAsync(semanticModel, entity).ConfigureAwait(false);
            processResult.AddRange(result);
        });

        return processResult;
    }

    public async Task<SemanticProcessResult> UpdateSemanticDescriptionAsync(
        SemanticModel semanticModel,
        SemanticModelEntity entity
    )
    {
        var startTime = DateTime.UtcNow;
        var processResult = new SemanticProcessResult();
        var entityType = entity.GetType().Name;
        var scope = $"{entityType} [{entity.Schema}].[{entity.Name}]";

        using (_logger.BeginScope(scope))
        {
            _logger.LogInformation(
                "{Message} {Type} [{Schema}].[{Name}]",
                _resourceManagerLogMessages.GetString("GenerateSemanticDescriptionForEntity"),
                entityType, entity.Schema, entity.Name);

            // Fetch sample data based on entity type
            List<Dictionary<string, object>> sampleData = [];
            IEnumerable<SemanticModelTable> relatedTables = Enumerable.Empty<SemanticModelTable>();

            switch (entity)
            {
                case SemanticModelTable table:
                    sampleData = await _schemaRepository.GetSampleTableDataAsync(
                        new TableInfo(table.Schema, table.Name), 5, true);
                    break;

                case SemanticModelView view:
                    // Fetch tables used in the view
                    var vwtableList = await GetTableListFromViewDefinitionAsync(semanticModel, view);
                    await UpdateTableSemanticDescriptionAsync(semanticModel, vwtableList);
                    relatedTables = semanticModel.SelectTables(vwtableList);

                    sampleData = await _schemaRepository.GetSampleViewDataAsync(
                        new ViewInfo(view.Schema, view.Name), 5, true);
                    break;

                case SemanticModelStoredProcedure storedProcedure:
                    // Fetch tables used in the stored procedure
                    var spTableList = await GetTableListFromStoredProcedureDefinitionAsync(semanticModel, storedProcedure);
                    await UpdateTableSemanticDescriptionAsync(semanticModel, spTableList);
                    relatedTables = semanticModel.SelectTables(spTableList);

                    // Stored procedures may not have sample data, so we can skip sample data fetching
                    break;
            }

            var sampleDataSerialized = SerializeSampleData(sampleData);

            var projectInfo = new
            {
                description = _project.Settings.Database.Description
            };

            var entityInfo = new
            {
                structure = entity.ToYaml(),
                data = sampleDataSerialized,
                definition = (entity as SemanticModelStoredProcedure)?.Definition,
                parameters = (entity as SemanticModelStoredProcedure)?.Parameters
            };


            var promptExecutionSettings = new PromptExecutionSettings
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ServiceId = "ChatCompletion"
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            };

            var arguments = new KernelArguments(promptExecutionSettings)
            {
                { "entity", entityInfo },
                { "project", projectInfo }
            };

            if (relatedTables.Any())
            {
                arguments.Add("tables", relatedTables);
            }

            var promptyFilename = $"describe_{entityType.ToLower()}.prompty";
            promptyFilename = Path.Combine(_promptyFolder, promptyFilename);

            var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();
#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var result = await semanticKernel.InvokeAsync(function, arguments);

            entity.SetSemanticDescription(result.ToString());

            var timeTaken = DateTime.UtcNow - startTime;
            var usage = result.Metadata?["Usage"] as OpenAI.Chat.ChatTokenUsage;
            processResult.Add(new SemanticProcessResultItem(scope, "ChatCompletion", usage, timeTaken));

            _logger.LogInformation(
                "{Message} {Type} [{Schema}].[{Name}]",
                _resourceManagerLogMessages.GetString("GeneratedSemanticDescriptionForEntity"),
                entityType, entity.Schema, entity.Name);
        }

        return processResult;
    }

    /// <summary>
    /// Generates semantic descriptions for all tables using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the semantic process summary.</returns>
    public async Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(semanticModel.Tables, GetParallelismOptions(), async (table, cancellationToken) =>
        {
            var result = await UpdateTableSemanticDescriptionAsync(semanticModel, table).ConfigureAwait(false);
            processResult.AddRange(result);
        });

        return processResult;
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of tables using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="tables"></param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the semantic process summary.</returns>
    public async Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(SemanticModel semanticModel, TableList tables)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(tables.Tables, GetParallelismOptions(), async (table, cancellationToken) =>
        {
            var semanticModelTable = semanticModel.Tables.FirstOrDefault(t => t.Schema == table.SchemaName && t.Name == table.TableName);
            if (semanticModelTable != null && string.IsNullOrEmpty(semanticModelTable.SemanticDescription))
            {
                _logger.LogInformation("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("TableMissingSemanticDescription"), table.SchemaName, table.TableName);
                var result = await UpdateTableSemanticDescriptionAsync(semanticModel, semanticModelTable).ConfigureAwait(false);
                processResult.AddRange(result);
            }
        });

        return processResult;
    }

    /// <summary>
    /// Generates a semantic description for the specified table using Semantic Kernel.
    /// </summary>
    /// <param name="table">The semantic model table for which to generate the description.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the semantic process summary.</returns>
    public async Task<SemanticProcessResult> UpdateTableSemanticDescriptionAsync(
        SemanticModel semanticModel,
        SemanticModelTable table
    )
    {
        return await UpdateSemanticDescriptionAsync(semanticModel, table);
    }

    /// <summary>
    /// Generates semantic descriptions for all views using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public async Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(semanticModel.Views, GetParallelismOptions(), async (view, cancellationToken) =>
        {
            var result = await UpdateViewSemanticDescriptionAsync(semanticModel, view).ConfigureAwait(false);
            processResult.AddRange(result);
        });

        return processResult;
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of views using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="views"></param>
    /// <returns></returns>
    public async Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(SemanticModel semanticModel, ViewList views)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(views.Views, GetParallelismOptions(), async (view, cancellationToken) =>
        {
            var semanticModelView = semanticModel.Views.FirstOrDefault(v => v.Schema == view.SchemaName && v.Name == view.ViewName);
            if (semanticModelView != null && string.IsNullOrEmpty(semanticModelView.SemanticDescription))
            {
                _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("ViewMissingSemanticDescription"), view.SchemaName, view.ViewName);
                var result = await UpdateViewSemanticDescriptionAsync(semanticModel, semanticModelView).ConfigureAwait(false);
                processResult.AddRange(result);
            }
        });

        return processResult;
    }

    /// <summary>
    /// Generates a semantic description for the specified view using Semantic Kernel.
    /// </summary>
    /// <param name="view">The semantic model view for which to generate the description.</param>
    public async Task<SemanticProcessResult> UpdateViewSemanticDescriptionAsync(
        SemanticModel semanticModel,
        SemanticModelView view
    )
    {
        return await UpdateSemanticDescriptionAsync(semanticModel, view);
    }

    /// <summary>
    /// Generates semantic descriptions for all stored procedures using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public async Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(semanticModel.StoredProcedures, GetParallelismOptions(), async (storedProcedure, cancellationToken) =>
        {
            var result = await UpdateStoredProcedureSemanticDescriptionAsync(semanticModel, storedProcedure).ConfigureAwait(false);
            processResult.AddRange(result);
        });

        return processResult;
    }

    /// <summary>
    /// Generates semantic descriptions for the specified list of stored procedures using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel"></param>
    /// <param name="storedProcedures"></param>
    /// <returns></returns>
    public async Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(SemanticModel semanticModel, StoredProcedureList storedProcedureList)
    {
        var processResult = new SemanticProcessResult();

        await Parallel.ForEachAsync(storedProcedureList.StoredProcedures, GetParallelismOptions(), async (storedProcedure, cancellationToken) =>
        {
            var semanticModelStoredProcedure = semanticModel.StoredProcedures.FirstOrDefault(sp => sp.Schema == storedProcedure.SchemaName && sp.Name == storedProcedure.ProcedureName);
            if (semanticModelStoredProcedure != null && string.IsNullOrEmpty(semanticModelStoredProcedure.SemanticDescription))
            {
                _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("StoredProcedureMissingSemanticDescription"), storedProcedure.SchemaName, storedProcedure.ProcedureName);
                var result = await UpdateStoredProcedureSemanticDescriptionAsync(semanticModel, semanticModelStoredProcedure).ConfigureAwait(false);
                processResult.AddRange(result);
            }
        });

        return processResult;
    }

    /// <summary>
    /// Generates a semantic description for the specified stored procedure using Semantic Kernel.
    /// </summary>
    /// <param name="storedProcedure">The semantic model stored procedure for which to generate the description.</param>
    public async Task<SemanticProcessResult> UpdateStoredProcedureSemanticDescriptionAsync(
        SemanticModel semanticModel,
        SemanticModelStoredProcedure storedProcedure
    )
    {
        return await UpdateSemanticDescriptionAsync(semanticModel, storedProcedure);
    }

    /// <summary>
    /// Gets a list of tables from the specified view definition using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="view">The view to get the list of tables from</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of tables.</returns>
    public async Task<TableList> GetTableListFromViewDefinitionAsync(SemanticModel semanticModel, SemanticModelView view)
    {
        _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("GetTableListFromViewDefinition"), view.Schema, view.Name);

        var promptyFilename = "get_tables_from_view_definition.prompty";
        promptyFilename = System.IO.Path.Combine(_promptyFolder, promptyFilename);
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
            { "entity", viewInfo }
        };

        var result = await semanticKernel.InvokeAsync(function, arguments);
        var resultString = result?.ToString();
        var tableList = new TableList();
        if (string.IsNullOrEmpty(resultString))
        {
            _logger.LogWarning("{Message}", _resourceManagerLogMessages.GetString("SemanticKernelReturnedEmptyResult"));
        }
        else
        {
            tableList = JsonSerializer.Deserialize<TableList>(resultString);
        }

        _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]. Table Count={Count}", _resourceManagerLogMessages.GetString("GotTableListFromViewDefinition"), view.Schema, view.Name, tableList.Tables.Count);

        return tableList;
    }

    /// <summary>
    /// Gets a list of tables from the specified stored procedure definition using Semantic Kernel.
    /// </summary>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="storedProcedure">The stored procedure to get the list of tables from</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of tables.</returns>
    public async Task<TableList> GetTableListFromStoredProcedureDefinitionAsync(SemanticModel semanticModel, SemanticModelStoredProcedure storedProcedure)
    {
        _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("GetTableListFromStoredProcedureDefinition"), storedProcedure.Schema, storedProcedure.Name);

        var promptyFilename = "get_tables_from_storedprocedure_definition.prompty";
        promptyFilename = System.IO.Path.Combine(_promptyFolder, promptyFilename);
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

        var storedProcedureInfo = new
        {
            definition = storedProcedure.Definition
        };
        var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "entity", storedProcedureInfo }
        };

        var result = await semanticKernel.InvokeAsync(function, arguments);
        var resultString = result?.ToString();
        var tableList = new TableList();
        if (string.IsNullOrEmpty(resultString))
        {
            _logger.LogWarning("{Message}]", _resourceManagerLogMessages.GetString("SemanticKernelReturnedEmptyResult"));
        }
        else
        {
            tableList = JsonSerializer.Deserialize<TableList>(resultString);
        }

        _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]. Table Count={Count}", _resourceManagerLogMessages.GetString("GotTableListFromStoredProcedureDefinition"), storedProcedure.Schema, storedProcedure.Name, tableList.Tables.Count);

        return tableList;
    }

    private static string SerializeSampleData(List<Dictionary<string, object>> sampleData)
    {
        const int MaxColumnLength = 200; // Adjust the maximum length as needed

        if (sampleData.Count > 0)
        {
            var truncatedData = sampleData.Select(row =>
            {
                var truncatedRow = new Dictionary<string, object?>();
                foreach (var kvp in row)
                {
                    truncatedRow[kvp.Key] = TruncateValue(kvp.Value, MaxColumnLength);
                }
                return truncatedRow;
            }).ToList();

            return JsonSerializer.Serialize(truncatedData);
        }

        return "No sample data available";
    }

    /// <summary>
    /// Truncates the value to the specified maximum length.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    private static object? TruncateValue(object value, int maxLength)
    {
        if (value is string strValue)
        {
            if (strValue.Length > maxLength)
                return strValue.Substring(0, maxLength) + "...";
            return strValue;
        }
        else if (value is byte[] byteArray)
        {
            // Convert byte array to base64 string
            string base64Str = Convert.ToBase64String(byteArray);
            if (base64Str.Length > maxLength)
                return base64Str.Substring(0, maxLength) + "...";
            return base64Str;
        }
        else if (value != null)
        {
            string valueStr = value.ToString() ?? string.Empty;
            if (valueStr.Length > maxLength)
                return valueStr.Substring(0, maxLength) + "...";
            return value;
        }
        else
        {
            return null;
        }
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