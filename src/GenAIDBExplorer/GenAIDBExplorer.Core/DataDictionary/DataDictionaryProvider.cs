using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.Models.Database;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using DocumentFormat.OpenXml.Vml.Office;

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
    private const string _promptyFolder = "Prompty";

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task ProcessTableDataDictionaryAsync(
        SemanticModel semanticModel,
        DirectoryInfo sourcePath,
        string? schemaName,
        string? tableName)
    {
        if (!sourcePath.Exists)
        {
            _logger.LogError("The source path '{SourcePath}' does not exist.", sourcePath.FullName);
            return;
        }

        var markdownFiles = Directory.GetFiles(sourcePath.FullName, "*.md", SearchOption.AllDirectories);

        if (markdownFiles.Length == 0)
        {
            _logger.LogWarning("No markdown files found in '{SourcePath}'", sourcePath.FullName);
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

            var existingTable = semanticModel.FindTable(table.SchemaName, table.TableName);
            if (existingTable != null)
            {
                _logger.LogInformation(
                    "Updating table '{Schema}.{Table}' in semantic model.",
                    table.SchemaName,
                    table.TableName);

                existingTable.Description = table.Description;
                existingTable.Details = table.Details;
                existingTable.AdditionalInformation = table.AdditionalInformation;
                // Update other properties if needed
            }
            else
            {
                _logger.LogWarning(
                    "Table '{Schema}.{Table}' does not exist in the semantic model.",
                    table.SchemaName,
                    table.TableName);
            }
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

