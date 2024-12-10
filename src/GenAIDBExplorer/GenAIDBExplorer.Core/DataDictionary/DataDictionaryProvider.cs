using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.Models.Database;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using DocumentFormat.OpenXml.Vml.Office;
using System.Resources;

namespace GenAIDBExplorer.Core.DataDictionary;

/// <summary>
/// Provides functionality to generate semantic model tables from data dictionary markdown files.
/// </summary>
public class DataDictionaryProvider(
        IProject project,
        ISemanticKernelFactory semanticKernelFactory,
        ILogger<DataDictionaryProvider> logger
    ) : IDataDictionaryProvider
{
    private readonly IProject _project = project;
    private readonly ISemanticKernelFactory _semanticKernelFactory = semanticKernelFactory;
    private readonly ILogger<DataDictionaryProvider> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(DataDictionaryProvider).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(DataDictionaryProvider).Assembly);
    private const string _promptyFolder = "Prompty";

    /// <summary>
    /// Enriches the semantic model by adding information from a data dictionary.
    /// </summary>
    /// <param name="semanticModel">The semantic model to update.</param>
    /// <param name="sourcePathPattern">The source path pattern to search for data dictionary files.</param>
    /// <param name="schemaName">The schema name to filter tables.</param>
    /// <param name="tableName">The table name to filter tables.</param>
    public async Task EnrichSemanticModelFromDataDictionaryAsync(
        SemanticModel semanticModel,
        string sourcePathPattern,
        string? schemaName,
        string? tableName)
    {
        var directory = Path.GetDirectoryName(sourcePathPattern);
        var searchPattern = Path.GetFileName(sourcePathPattern);

        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        if (string.IsNullOrEmpty(searchPattern))
        {
            searchPattern = "*.md";
        }

        if (!Directory.Exists(directory))
        {
            _logger.LogError("{ErrorMessage} '{SourcePath}'", _resourceManagerErrorMessages.GetString("ErrorDataDictionarySourcePathDoesNotExist"), directory);
            return;
        }

        var markdownFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);

        if (markdownFiles.Length == 0)
        {
            _logger.LogWarning("{Message} '{SourcePath}' with pattern '{SearchPattern}'", _resourceManagerLogMessages.GetString("DataDictionaryFilesNotFound"), directory, searchPattern);
            return;
        }

        var tables = await GetTablesFromMarkdownFilesAsync(markdownFiles);

        foreach (var table in tables)
        {
            if (!string.IsNullOrEmpty(schemaName) &&
                !table.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tableName) &&
                !table.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var semanticModelTable = semanticModel.FindTable(table.SchemaName, table.TableName);
            if (semanticModelTable != null)
            {
                logger.LogInformation(
                    "{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("EnrichingTableFromDataDictionary"), table.SchemaName, table.TableName);

                MergeDataDictionaryTableIntoSemanticModel(semanticModelTable, table);
            }
            else
            {
                _logger.LogWarning("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("TableDoesNotExistInSemanticModel"), table.SchemaName, table.TableName);
            }
        }
    }

    /// <summary>
    /// Merges a data dictionary table into a semantic model table.
    /// </summary>
    /// <param name="semanticModelTable"></param>
    /// <param name="dataDictionaryTable"></param>
    internal void MergeDataDictionaryTableIntoSemanticModel(SemanticModelTable semanticModelTable, TableDataDictionary dataDictionaryTable)
    {
        semanticModelTable.Description = dataDictionaryTable.Description;
        semanticModelTable.Details = dataDictionaryTable.Details;
        semanticModelTable.AdditionalInformation = dataDictionaryTable.AdditionalInformation;

        var dataDictionaryColumnNames = dataDictionaryTable.Columns.Select(c => c.ColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var semanticModelColumnNames = semanticModelTable.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Warn about columns in the data dictionary but not in the semantic model table
        foreach (var column in dataDictionaryTable.Columns)
        {
            var semanticModelColumn = semanticModelTable.Columns.FirstOrDefault(c => c.Name.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));
            if (semanticModelColumn != null)
            {
                if (!semanticModelColumn.Type.Equals(column.Type, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Column type mismatch for column '{ColumnName}' in table '{TableName}'. Data dictionary type: '{DataType}', Semantic model type: '{SemanticType}'",
                        column.ColumnName, semanticModelTable.Name, column.Type, semanticModelColumn.Type);
                }
                semanticModelColumn.Description = column.Description;
            }
            else
            {
                _logger.LogWarning("Column '{ColumnName}' from data dictionary not found in semantic model table '{TableName}'", column.ColumnName, semanticModelTable.Name);
            }
        }

        // Warn about columns in the semantic model table but not in the data dictionary
        foreach (var column in semanticModelTable.Columns)
        {
            if (!dataDictionaryColumnNames.Contains(column.Name))
            {
                _logger.LogWarning("Column '{ColumnName}' in semantic model table '{TableName}' not found in data dictionary", column.Name, semanticModelTable.Name);
            }
        }
    }

    /// <summary>
    /// Extracts tables from a collection of markdown files.
    /// </summary>
    /// <param name="markdownFiles"></param>
    /// <returns></returns>
    internal async Task<List<TableDataDictionary>> GetTablesFromMarkdownFilesAsync(IEnumerable<string> markdownFiles)
    {
        var processResult = new List<TableDataDictionary>();
        var parallelOptions = GetParallelismOptions();

        await Parallel.ForEachAsync(markdownFiles, parallelOptions, async (filePath, cancellationToken) =>
        {
            var markdownContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var table = await GetTableFromMarkdownAsync(markdownContent);
            lock (processResult)
            {
                processResult.Add(table);
            }
        });

        return processResult;
    }

    /// <summary>
    /// Extracts a table structure from the given markdown data dictionary file.
    /// </summary>
    /// <param name="markdownContent"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal async Task<TableDataDictionary> GetTableFromMarkdownAsync(string markdownContent)
    {
        // Initialize Semantic Kernel
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Load the prompty function
        var promptyFilename = Path.Combine(_promptyFolder, "get_table_from_data_dictionary_markdown.prompty");
#pragma warning disable SKEXP0040 // Experimental API
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Experimental API

        var entityInfo = new
        {
            markdown = markdownContent
        };

        // Set up prompt execution settings
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
#pragma warning disable SKEXP0001 // Experimental API
            ServiceId = "ChatCompletionStructured",
#pragma warning restore SKEXP0001 // Experimental API
#pragma warning disable SKEXP0010 // Experimental API
            ResponseFormat = typeof(TableDataDictionary)
#pragma warning restore SKEXP0010 // Experimental API
        };

        // Prepare arguments
        var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "entity", entityInfo }
        };

        // Invoke the function
        var result = await semanticKernel.InvokeAsync(function, arguments);

        var resultString = result?.ToString();

        if (string.IsNullOrEmpty(resultString))
        {
            _logger.LogWarning("Semantic Kernel returned an empty result for markdown content.");
            throw new InvalidOperationException("Failed to extract table structure from markdown content.");
        }
        else
        {
            // Deserialize the result into a SemanticModelTable
            var table = JsonSerializer.Deserialize<TableDataDictionary>(resultString)
                ?? throw new InvalidOperationException("Failed to deserialize table structure from markdown content.");

            return table;
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

