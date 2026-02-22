namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Extension method to apply usage settings to a collection of <see cref="SemanticModelColumn"/> objects.
/// </summary>
public static class SemanticModelColumnExtensions
{
    public static void ApplyUsageSettings(this IEnumerable<SemanticModelColumn> columns, IColumnUsageStrategy strategy, IEnumerable<string> regexPatterns)
    {
        foreach (var column in columns)
        {
            strategy.ApplyUsageSettings(column, regexPatterns);
        }
    }
}
