namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Configuration for the query-model agent and guardrails.
/// </summary>
public class QueryModelSettings
{
    /// <summary>
    /// The settings key that contains the QueryModel settings.
    /// </summary>
    public const string PropertyName = "QueryModel";

    /// <summary>
    /// Gets or sets the name for the Foundry agent version.
    /// </summary>
    public string AgentName { get; set; } = "genaidb-query-agent";

    /// <summary>
    /// Gets or sets the system instructions for the agent.
    /// If null, uses default from prompt template.
    /// </summary>
    public string? AgentInstructions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of response rounds before forced termination.
    /// </summary>
    public int MaxResponseRounds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum cumulative token budget.
    /// </summary>
    public long MaxTokenBudget { get; set; } = 100_000;

    /// <summary>
    /// Gets or sets the maximum wall-clock time in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default number of results per search tool call.
    /// </summary>
    public int DefaultTopK { get; set; } = 5;
}
