namespace GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;

/// <summary>
/// Represents a strategy for determining whether an entity is used based on a set of regular expressions.
/// </summary>
/// <typeparam name="T">The type of the semantic model entity.</typeparam>
public interface IEntityUsageStrategy<T> where T : ISemanticModelEntity
{
    void ApplyUsageSettings(T entity, IEnumerable<string> regexPatterns);
}
