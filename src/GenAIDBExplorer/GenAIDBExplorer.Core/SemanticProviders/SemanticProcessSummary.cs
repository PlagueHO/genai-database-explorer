namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Represents a summary of semantic process results.
/// </summary>
public class SemanticProcessSummary
{
    public List<SemanticProcessResult> Results { get; } = [];

    /// <summary>
    /// Adds a SemanticProcessResult to the summary.
    /// </summary>
    /// <param name="result">The result to add.</param>
    public void AddResult(SemanticProcessResult result)
    {
        var existingResult = Results.FirstOrDefault(r => r.Name == result.Name);
        if (existingResult != null)
        {
            existingResult.AddResult(result);
            return;
        }
        Results.Add(result);
    }
    /// <summary>
    /// Adds the values from another SemanticProcessSummary to this instance.
    /// </summary>
    /// <param name="summary">The summary to add.</param>
    public void AddSummary(SemanticProcessSummary summary)
    {
        Results.AddRange(summary.Results);
    }
    /// <summary>
    /// Gets the total request count of all results in the summary.
    /// </summary>
    /// <returns>The total request count.</returns>
    public int GetTotalRequestCount()
    {
        return Results.Sum(r => r.RequestCount);
    }
    /// <summary>
    /// Gets the total tokens in of all results in the summary.
    /// </summary>
    /// <returns>The total tokens in.</returns>
    public int GetTotalTokensIn()
    {
        return Results.Sum(r => r.TokensIn);
    }
    /// <summary>
    /// Gets the total tokens out of all results in the summary.
    /// </summary>
    /// <returns>The total tokens out.</returns>
    public int GetTotalTokensOut()
    {
        return Results.Sum(r => r.TokensOut);
    }
    /// <summary>
    /// Gets the total time taken of all results in the summary.
    /// </summary>
    /// <returns>The total time taken.</returns>
    public TimeSpan GetTotalTimeTaken()
    {
        return Results.Aggregate(TimeSpan.Zero, (acc, r) => acc + r.TimeTaken);
    }
}
