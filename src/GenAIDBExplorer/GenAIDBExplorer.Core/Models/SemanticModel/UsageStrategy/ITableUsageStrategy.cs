namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether a table is used based on a set of regular expressions.
/// </summary>
public interface ITableUsageStrategy
{
    void ApplyUsageSettings(SemanticModelTable table, IEnumerable<string> regexPatterns);
}
