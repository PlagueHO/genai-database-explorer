namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether a column is used.
/// </summary>
public interface IColumnUsageStrategy
{
    void ApplyUsageSettings(SemanticModelColumn column, IEnumerable<string> regexPatterns);
}
