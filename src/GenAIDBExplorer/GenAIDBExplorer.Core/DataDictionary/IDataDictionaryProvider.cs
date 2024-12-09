using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.DataDictionary;

/// <summary>
/// Provides functionality to generate semantic model tables from data dictionary markdown files.
/// </summary>
public interface IDataDictionaryProvider
{
    /// <summary>
    /// Processes table data dictionary files and updates the semantic model.
    /// </summary>
    /// <param name="semanticModel">The semantic model to update.</param>
    /// <param name="sourcePath">The directory containing data dictionary files.</param>
    /// <param name="schemaName">The schema name to filter tables.</param>
    /// <param name="tableName">The table name to filter tables.</param>
    Task ProcessTableDataDictionaryAsync(SemanticModel semanticModel, DirectoryInfo sourcePath, string? schemaName, string? tableName);
}