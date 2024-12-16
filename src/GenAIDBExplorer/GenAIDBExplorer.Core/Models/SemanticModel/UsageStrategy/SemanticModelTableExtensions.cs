namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Extension method to apply usage settings to a <see cref="SemanticModelTable"/> object.
/// </summary>
public static class SemanticModelTableExtensions
{
    public static void ApplyUsageSettings(this SemanticModelTable table, ITableUsageStrategy strategy, IEnumerable<string> regexPatterns)
    {
        strategy.ApplyUsageSettings(table, regexPatterns);
    }
}
