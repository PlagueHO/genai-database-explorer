using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.DataDictionary;

/// <summary>
/// Provides functionality to enrich semantic model entities from data dictionary files.
/// </summary>
public interface IDataDictionaryProvider
{
    Task EnrichSemanticModelFromDataDictionaryAsync(SemanticModel semanticModel, string sourcePathPattern, string? schemaName, string? tableName);
}