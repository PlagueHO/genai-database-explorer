using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace GenAIDBExplorer.Core.DataDictionaryProviders;

/// <summary>
/// Provides functionality to generate semantic model tables from data dictionary markdown files.
/// </summary>
public class DataDictionaryProvider : IDataDictionaryProvider
{
    private readonly ISemanticKernelFactory _semanticKernelFactory;
    private readonly ILogger<DataDictionaryProvider> _logger;
    private const string _promptyFolder = "Prompty";

    public DataDictionaryProvider(
        ISemanticKernelFactory semanticKernelFactory,
        ILogger<DataDictionaryProvider> logger)
    {
        _semanticKernelFactory = semanticKernelFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<SemanticModelTable>> GetTablesFromMarkdownFilesAsync(IEnumerable<string> markdownFiles)
    {
        var tasks = markdownFiles.Select(async filePath =>
        {
            var markdownContent = await File.ReadAllTextAsync(filePath);
            return await GetTableFromMarkdownAsync(markdownContent);
        });

        return [.. (await Task.WhenAll(tasks))];
    }

    /// <inheritdoc/>
    public async Task<SemanticModelTable> GetTableFromMarkdownAsync(string markdownContent)
    {
        // Initialize Semantic Kernel
        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();

        // Load the prompty function
        var promptyFilename = Path.Combine(_promptyFolder, "extract_table_structure_from_markdown.prompty");
#pragma warning disable SKEXP0040 // Experimental API
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
#pragma warning restore SKEXP0040 // Experimental API

        // Set up prompt execution settings
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
#pragma warning disable SKEXP0001 // Experimental API
            ServiceId = "ChatCompletionStructured",
#pragma warning restore SKEXP0001 // Experimental API
#pragma warning disable SKEXP0010 // Experimental API
            ResponseFormat = typeof(SemanticModelTable)
#pragma warning restore SKEXP0010 // Experimental API
        };

        // Prepare arguments
        var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "markdownContent", markdownContent }
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
            var table = JsonSerializer.Deserialize<SemanticModelTable>(resultString)
                ?? throw new InvalidOperationException("Failed to deserialize table structure from markdown content.");

            return table;
        }
    }
}
