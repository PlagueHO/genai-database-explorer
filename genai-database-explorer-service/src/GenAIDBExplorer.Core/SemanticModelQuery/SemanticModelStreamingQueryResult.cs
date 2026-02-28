using System.Runtime.CompilerServices;

namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Wrapper returned by QueryStreamingAsync. Provides access to the token stream
/// and, once the stream completes, the full query metadata.
/// </summary>
public sealed class SemanticModelStreamingQueryResult : IAsyncDisposable
{
    private readonly TaskCompletionSource<SemanticModelQueryResult> _metadataSource;
    private readonly IAsyncEnumerable<string> _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticModelStreamingQueryResult"/> class.
    /// </summary>
    /// <param name="tokens">The token stream.</param>
    /// <param name="metadataSource">The source for metadata completion.</param>
    public SemanticModelStreamingQueryResult(
        IAsyncEnumerable<string> tokens,
        TaskCompletionSource<SemanticModelQueryResult> metadataSource)
    {
        _tokens = tokens;
        _metadataSource = metadataSource;
    }

    /// <summary>
    /// Stream of answer text tokens for real-time display.
    /// Must be fully enumerated before calling GetMetadataAsync.
    /// </summary>
    public IAsyncEnumerable<string> Tokens => _tokens;

    /// <summary>
    /// Returns the full query result (entities, rounds, token usage, termination reason)
    /// after the Tokens stream has been fully consumed.
    /// </summary>
    public Task<SemanticModelQueryResult> GetMetadataAsync() => _metadataSource.Task;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
