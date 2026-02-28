using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace GenAIDBExplorer.Core.SemanticModelQuery;

/// <summary>
/// Orchestrates agent-powered natural language queries against a semantic model's vector index.
/// Uses the Microsoft Agent Framework with function tools backed by <see cref="ISemanticModelSearchService"/>.
/// </summary>
public sealed class SemanticModelQueryService(
    IProject project,
    ISemanticModelSearchService searchService,
    IChatClientFactory chatClientFactory,
    IPromptTemplateParser promptTemplateParser,
    ILiquidTemplateRenderer liquidTemplateRenderer,
    ILoggerFactory loggerFactory,
    ILogger<SemanticModelQueryService> logger
) : ISemanticModelQueryService
{
    private readonly IProject _project = project;
    private readonly ISemanticModelSearchService _searchService = searchService;
    private readonly IChatClientFactory _chatClientFactory = chatClientFactory;
    private readonly IPromptTemplateParser _promptTemplateParser = promptTemplateParser;
    private readonly ILiquidTemplateRenderer _liquidTemplateRenderer = liquidTemplateRenderer;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<SemanticModelQueryService> _logger = logger;

    private AIAgent? _agent;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private static readonly AsyncLocal<QueryContext?> _queryContext = new();

    /// <summary>
    /// Per-query state flowed via AsyncLocal so concurrent queries don't interfere.
    /// </summary>
    private sealed class QueryContext
    {
        public List<SemanticModelSearchResult> ReferencedEntities { get; } = [];
        public int DefaultTopK { get; init; }
    }

    /// <inheritdoc />
    public async Task<SemanticModelQueryResult> QueryAsync(
        SemanticModelQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question, nameof(request.Question));

        var streamingResult = await QueryStreamingAsync(request, cancellationToken);
        await using (streamingResult.ConfigureAwait(false))
        {
            var answerBuilder = new StringBuilder();
            await foreach (var token in streamingResult.Tokens.WithCancellation(cancellationToken))
            {
                answerBuilder.Append(token);
            }

            var metadata = await streamingResult.GetMetadataAsync();
            return metadata with { Answer = answerBuilder.ToString() };
        }
    }

    /// <inheritdoc />
    public async Task<SemanticModelStreamingQueryResult> QueryStreamingAsync(
        SemanticModelQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question, nameof(request.Question));

        var agent = await EnsureAgentInitializedAsync(cancellationToken);
        var settings = _project.Settings.QueryModel;
        var metadataSource = new TaskCompletionSource<SemanticModelQueryResult>();
        var channel = Channel.CreateUnbounded<string>();

        // Start the agent loop in the background, writing tokens to the channel
        _ = RunAgentLoopAsync(agent, request, settings, metadataSource, channel.Writer, cancellationToken);

        var tokens = ReadChannelAsync(channel.Reader, cancellationToken);
        return new SemanticModelStreamingQueryResult(tokens, metadataSource);
    }

    private async Task RunAgentLoopAsync(
        AIAgent agent,
        SemanticModelQueryRequest request,
        QueryModelSettings settings,
        TaskCompletionSource<SemanticModelQueryResult> metadataSource,
        ChannelWriter<string> writer,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var answerBuilder = new StringBuilder();
        var context = new QueryContext
        {
            DefaultTopK = request.TopK ?? settings.DefaultTopK
        };
        _queryContext.Value = context;
        var responseRounds = 0;
        long inputTokens = 0;
        long outputTokens = 0;
        var terminationReason = QueryTerminationReason.Completed;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            _logger.LogInformation("Query model started for question: {Question}", request.Question);

            var session = await agent.CreateSessionAsync(timeoutCts.Token);

            await foreach (var update in agent.RunStreamingAsync(
                request.Question, session, cancellationToken: timeoutCts.Token))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    answerBuilder.Append(update.Text);
                    await writer.WriteAsync(update.Text, timeoutCts.Token);
                }

                // Track function calls for round counting
                if (update.Contents != null)
                {
                    foreach (var content in update.Contents)
                    {
                        if (content is FunctionCallContent)
                        {
                            responseRounds++;
                        }

                        if (content is UsageContent usageContent)
                        {
                            inputTokens += usageContent.Details.InputTokenCount ?? 0;
                            outputTokens += usageContent.Details.OutputTokenCount ?? 0;
                        }
                    }
                }

                // Check guardrails
                var totalTokens = inputTokens + outputTokens;
                if (totalTokens > settings.MaxTokenBudget)
                {
                    terminationReason = QueryTerminationReason.TokenBudgetExceeded;
                    _logger.LogWarning("Token budget exceeded: {TotalTokens} > {MaxTokenBudget}",
                        totalTokens, settings.MaxTokenBudget);
                    break;
                }

                if (responseRounds >= settings.MaxResponseRounds)
                {
                    terminationReason = QueryTerminationReason.MaxRoundsReached;
                    _logger.LogWarning("Max response rounds reached: {ResponseRounds} >= {MaxResponseRounds}",
                        responseRounds, settings.MaxResponseRounds);
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            terminationReason = QueryTerminationReason.TimeLimitExceeded;
            _logger.LogWarning("Query timed out after {TimeoutSeconds} seconds", settings.TimeoutSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            terminationReason = QueryTerminationReason.Error;
            _logger.LogError(ex, "Error during query execution");
        }
        finally
        {
            _queryContext.Value = null;
            writer.Complete();
        }

        stopwatch.Stop();
        var totalTokensFinal = inputTokens + outputTokens;

        // Deduplicate referenced entities by schema + name + entity type
        var distinctEntities = context.ReferencedEntities
            .GroupBy(e => $"{e.EntityType}|{e.SchemaName}|{e.EntityName}".ToUpperInvariant())
            .Select(g => g.OrderByDescending(e => e.Score).First())
            .ToList()
            .AsReadOnly();

        var result = new SemanticModelQueryResult(
            Answer: answerBuilder.ToString(),
            ReferencedEntities: distinctEntities,
            ResponseRounds: responseRounds,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            TotalTokens: totalTokensFinal,
            Duration: stopwatch.Elapsed,
            TerminationReason: terminationReason);

        metadataSource.TrySetResult(result);

        _logger.LogInformation(
            "Query completed. Rounds: {ResponseRounds}, Tokens: {TotalTokens}, Duration: {Duration:F1}s, Termination: {TerminationReason}",
            responseRounds, totalTokensFinal, stopwatch.Elapsed.TotalSeconds, terminationReason);
    }

    private static async IAsyncEnumerable<string> ReadChannelAsync(
        ChannelReader<string> reader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var token in reader.ReadAllAsync(cancellationToken))
        {
            yield return token;
        }
    }

    private async Task<AIAgent> EnsureAgentInitializedAsync(CancellationToken cancellationToken)
    {
        if (_agent is not null)
            return _agent;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_agent is not null)
                return _agent;

            _agent = CreateAgent();
            _logger.LogInformation("Query model agent created: {AgentName}", _project.Settings.QueryModel.AgentName);
            return _agent;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private AIAgent CreateAgent()
    {
        var settings = _project.Settings.QueryModel;

        // Create function tools that capture the search service
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                (string query, int topK = 0) => SearchAndTrackAsync("Table", query, topK),
                "searchTables",
                "Search for database tables matching the query using vector similarity. Returns JSON array of matching tables with schema, name, content, and similarity score."),

            AIFunctionFactory.Create(
                (string query, int topK = 0) => SearchAndTrackAsync("View", query, topK),
                "searchViews",
                "Search for database views matching the query using vector similarity. Returns JSON array of matching views with schema, name, content, and similarity score."),

            AIFunctionFactory.Create(
                (string query, int topK = 0) => SearchAndTrackAsync("StoredProcedure", query, topK),
                "searchStoredProcedures",
                "Search for stored procedures matching the query using vector similarity. Returns JSON array of matching stored procedures with schema, name, content, and similarity score."),
        };

        // Build agent instructions from prompt template or settings override
        var instructions = settings.AgentInstructions ?? BuildInstructionsFromTemplate();

        // Create an AIAgent from the chat client
        var chatClient = _chatClientFactory.CreateChatClient();
        var openAiChatClient = chatClient.GetService<OpenAI.Chat.ChatClient>();
        if (openAiChatClient is null)
        {
            throw new InvalidOperationException(
                "The IChatClient does not wrap an OpenAI ChatClient. " +
                "Ensure the ChatClientFactory creates an OpenAI-backed client.");
        }

        var agent = OpenAI.Chat.OpenAIChatClientExtensions.AsAIAgent(
            openAiChatClient,
            name: settings.AgentName,
            instructions: instructions,
            description: "Semantic model query agent for database exploration",
            tools: tools,
            loggerFactory: _loggerFactory);

        return agent;
    }

    private string BuildInstructionsFromTemplate()
    {
        try
        {
            var promptFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "PromptTemplates",
                "QueryModelAgent.prompt");

            var template = _promptTemplateParser.ParseFromFile(promptFilePath);
            var variables = new Dictionary<string, object?>
            {
                ["database_name"] = _project.Settings.Database.Name ?? "Unknown",
                ["database_description"] = _project.Settings.Database.Description ?? "No description available"
            };

            var messages = _liquidTemplateRenderer.RenderMessages(template, variables);
            // Extract the system message content
            var systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System);
            return systemMessage?.Text ?? "You are a database schema assistant.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load prompt template, using fallback instructions");
            return "You are a database schema expert assistant. Help users understand database schemas by searching the semantic model.";
        }
    }

    private async Task<string> SearchAndTrackAsync(string entityType, string query, int topK)
    {
        var context = _queryContext.Value;
        var effectiveTopK = topK > 0 ? topK : (context?.DefaultTopK ?? _project.Settings.QueryModel.DefaultTopK);

        _logger.LogDebug("Search tool called: EntityType={EntityType}, Query={Query}, TopK={TopK}",
            entityType, query, effectiveTopK);

        IReadOnlyList<SemanticModelSearchResult> results = entityType switch
        {
            "Table" => await _searchService.SearchTablesAsync(query, effectiveTopK),
            "View" => await _searchService.SearchViewsAsync(query, effectiveTopK),
            "StoredProcedure" => await _searchService.SearchStoredProceduresAsync(query, effectiveTopK),
            _ => []
        };

        // Track referenced entities for the current query
        context?.ReferencedEntities.AddRange(results);

        // Return results as JSON for the agent to process
        var resultData = results.Select(r => new
        {
            r.EntityType,
            r.SchemaName,
            r.EntityName,
            r.Content,
            r.Score
        });

        return JsonSerializer.Serialize(resultData, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_agent is not null)
        {
            _logger.LogInformation("Query model agent disposed: {AgentName}", _project.Settings.QueryModel.AgentName);
            _agent = null;
        }

        _initLock.Dispose();
        await ValueTask.CompletedTask;
    }
}
