namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Extension methods to apply usage settings to a <see cref="ISemanticModelEntity"/> object.
/// </summary>
public static class SemanticModelEntityUsageExtensions
{
    public static void ApplyUsageSettings<T>(this T entity, IEntityUsageStrategy<T> strategy, IEnumerable<string> regexPatterns) where T : ISemanticModelEntity
    {
        strategy.ApplyUsageSettings(entity, regexPatterns);
    }
}
