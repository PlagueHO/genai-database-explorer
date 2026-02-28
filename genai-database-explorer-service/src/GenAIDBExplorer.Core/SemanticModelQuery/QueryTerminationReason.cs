namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Describes why the agent query loop terminated.
/// </summary>
public enum QueryTerminationReason
{
    /// <summary>
    /// Agent produced a final answer naturally.
    /// </summary>
    Completed,

    /// <summary>
    /// Hit the configured maximum response rounds limit.
    /// </summary>
    MaxRoundsReached,

    /// <summary>
    /// Hit the configured maximum token budget.
    /// </summary>
    TokenBudgetExceeded,

    /// <summary>
    /// Hit the configured timeout limit.
    /// </summary>
    TimeLimitExceeded,

    /// <summary>
    /// An error occurred (partial results may be returned).
    /// </summary>
    Error
}
