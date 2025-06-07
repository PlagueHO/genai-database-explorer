using System.Collections;

namespace GenAIDBExplorer.Core.SemanticProviders;

/// <summary>
/// Represents a semantic process result.
/// Used for reporting back the results of a semantic process.
/// </summary>
public class SemanticProcessResult : IEnumerable<SemanticProcessResultItem>
{
    /// <summary>
    /// The list of semantic process result items.
    /// </summary>
    private readonly List<SemanticProcessResultItem> _items = [];

    /// <summary>
    /// Adds the specified semantic process result item.
    /// </summary>
    /// <param name="item"></param>
    public void Add(SemanticProcessResultItem item)
    {
        _items.Add(item);
    }

    /// <summary>
    /// Adds the specified semantic process result items.
    /// </summary>
    /// <param name="items"></param>
    public void AddRange(IEnumerable<SemanticProcessResultItem> items)
    {
        _items.AddRange(items);
    }

    /// <summary>
    /// Gets the total input token count.
    /// </summary>
    /// <returns></returns>
    public int GetTotalInputTokenCount()
    {
        return _items.Sum(item => item.TokenUsage.InputTokenCount);
    }

    /// <summary>
    /// Gets the total input token count for a specific label.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public int GetTotalInputTokenCount(string label)
    {
        return _items.Where(item => item.Label == label).Sum(item => item.TokenUsage.InputTokenCount);
    }

    /// <summary>
    /// Gets the total output token count.
    /// </summary>
    /// <returns></returns>
    public int GetTotalOutputTokenCount()
    {
        return _items.Sum(item => item.TokenUsage.OutputTokenCount);
    }

    /// <summary>
    /// Gets the total output token count for a specific label.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public int GetTotalOutputTokenCount(string label)
    {
        return _items.Where(item => item.Label == label).Sum(item => item.TokenUsage.OutputTokenCount);
    }

    /// <summary>
    /// Gets the total token count.
    /// </summary>
    /// <returns></returns>
    public int GetTotalTokenCount()
    {
        return _items.Sum(item => item.TokenUsage.TotalTokenCount);
    }

    /// <summary>
    /// Gets the total token count for a specific label.
    /// </summary>
    /// <param name="other"></param>
    public int GetTotalTokenCount(string label)
    {
        return _items.Where(item => item.Label == label).Sum(item => item.TokenUsage.TotalTokenCount);
    }

    /// <summary>
    /// Appends the specified semantic process result.
    /// </summary>
    /// <param name="other"></param>
    public void Append(SemanticProcessResult other)
    {
        _items.AddRange(other._items);
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<SemanticProcessResultItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Represents a semantic process result item.
/// </summary>
/// <param name="label"></param>
/// <param name="tokenUsage"></param>
/// <param name="timeTaken"></param>
public class SemanticProcessResultItem(
    string id,
    string label,
    OpenAI.Chat.ChatTokenUsage tokenUsage,
    TimeSpan timeTaken
)
{
    public string Id { get; set; } = id;
    public string Label { get; set; } = label;
    public OpenAI.Chat.ChatTokenUsage TokenUsage { get; set; } = tokenUsage;
    public TimeSpan TimeTaken { get; set; } = timeTaken;
}
