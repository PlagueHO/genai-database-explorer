namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// A natural language query request.
/// </summary>
/// <param name="Question">The natural language question.</param>
/// <param name="TopK">Override for default top-K search results. Falls back to QueryModelSettings.DefaultTopK.</param>
public sealed record SemanticModelQueryRequest(
    string Question,
    int? TopK = null);
