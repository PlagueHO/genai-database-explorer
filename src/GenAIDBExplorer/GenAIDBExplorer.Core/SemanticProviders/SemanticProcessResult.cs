namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Represents the result of a semantic process.
/// </summary>
public class SemanticProcessResult(
    string name,
    int requestCount = 0,
    int tokensIn = 0,
    int tokensOut = 0,
    TimeSpan timeTaken = new TimeSpan()
)
{
    public string Name { get; } = name;
    public int RequestCount { get; set; } = requestCount;
    public int TokensIn { get; set; } = tokensIn;
    public int TokensOut { get; set; } = tokensOut;
    public TimeSpan TimeTaken { get; set; } = timeTaken;

    /// <summary>
    /// Add a request to the result.
    /// </summary>
    /// <param name="tokensIn"></param>
    /// <param name="tokensOut"></param>
    /// <param name="timeTaken"></param>
    public void AddRequest(int tokensIn, int tokensOut, TimeSpan timeTaken)
    {
        RequestCount++;
        TokensIn += tokensIn;
        TokensOut += tokensOut;
        TimeTaken += timeTaken;
    }

    /// <summary>
    /// Adds the values from another SemanticProcessResult to this instance.
    /// </summary>
    /// <param name="result">The result SemanticProcessResult instance.</param>
    public void AddResult(SemanticProcessResult result)
    {
        RequestCount += result.RequestCount;
        TokensIn += result.TokensIn;
        TokensOut += result.TokensOut;
        TimeTaken += result.TimeTaken;
    }
}