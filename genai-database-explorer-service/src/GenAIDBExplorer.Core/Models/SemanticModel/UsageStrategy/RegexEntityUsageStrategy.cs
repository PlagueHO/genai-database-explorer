using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether an entity is used based on a set of regular expressions.
/// </summary>
/// <typeparam name="T">The type of the semantic model entity.</typeparam>
public class RegexEntityUsageStrategy<T> : IEntityUsageStrategy<T> where T : ISemanticModelEntity
{
    public void ApplyUsageSettings(T entity, IEnumerable<string> regexPatterns)
    {
        foreach (var pattern in regexPatterns)
        {
            if (Regex.IsMatch($"{entity.Schema}.{entity.Name}", pattern))
            {
                entity.NotUsed = true;
                entity.NotUsedReason = $"Matches pattern: {pattern}";
                return;
            }
        }
        entity.NotUsed = false;
        entity.NotUsedReason = null;
    }
}
