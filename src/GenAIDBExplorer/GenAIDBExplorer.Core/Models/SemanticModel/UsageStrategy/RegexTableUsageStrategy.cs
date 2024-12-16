using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether a table is used based on a set of regular expressions.
/// </summary>
public class RegexTableUsageStrategy : ITableUsageStrategy
{
    public void ApplyUsageSettings(SemanticModelTable table, IEnumerable<string> regexPatterns)
    {
        foreach (var pattern in regexPatterns)
        {
            if (Regex.IsMatch($"{table.Schema}.{table.Name}", pattern))
            {
                table.NotUsed = true;
                table.NotUsedReason = $"Matches pattern: {pattern}";
                return;
            }
        }
        table.NotUsed = false;
        table.NotUsedReason = null;
    }
}
