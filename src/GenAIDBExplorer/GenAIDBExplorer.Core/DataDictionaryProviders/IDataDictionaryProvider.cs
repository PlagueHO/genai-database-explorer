using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.DataDictionaryProviders;

/// <summary>
/// Provides functionality to generate semantic model tables from data dictionary markdown files.
/// </summary>
public interface IDataDictionaryProvider
{
    /// <summary>
    /// Parses a list of markdown files and returns a list of semantic model tables.
    /// </summary>
    /// <param name="markdownFiles">The list of markdown file paths.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of semantic model tables.</returns>
    Task<List<SemanticModelTable>> GetTablesFromMarkdownFilesAsync(IEnumerable<string> markdownFiles);

    /// <summary>
    /// Parses a markdown string and returns a semantic model table.
    /// </summary>
    /// <param name="markdownContent">The markdown content as a string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the semantic model table.</returns>
    Task<SemanticModelTable> GetTableFromMarkdownAsync(string markdownContent);
}
