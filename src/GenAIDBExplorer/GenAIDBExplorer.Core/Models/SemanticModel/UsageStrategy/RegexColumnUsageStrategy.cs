using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether a column is used based on a set of regular expressions.
/// </summary>
public class RegexColumnUsageStrategy : IColumnUsageStrategy
{
    /// <summary>
    /// Applies usage settings to the specified column based on the specified regular expression patterns.
    /// </summary>
    /// <param name="column"></param>
    /// <param name="regexPatterns"></param>
    public void ApplyUsageSettings(SemanticModelColumn column, IEnumerable<string> regexPatterns)
    {
        foreach (var pattern in regexPatterns)
        {
            if (Regex.IsMatch(column.Name, pattern))
            {
                column.NotUsed = true;
                column.NotUsedReason = $"Matches pattern: {pattern}";
                return;
            }
        }
        column.NotUsed = false;
        column.NotUsedReason = null;
    }
}
