using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticModelQuery;
using GenAIDBExplorer.Core.SemanticProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for querying a project's semantic model using natural language.
/// Delegates to <see cref="ISemanticModelQueryService"/> for agent-powered query execution.
/// </summary>
public class QueryModelCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelQueryService queryService,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<QueryModelCommandHandlerOptions>> logger
) : CommandHandler<QueryModelCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(QueryModelCommandHandler).Assembly);
    private readonly ISemanticModelQueryService _queryService = queryService;

    /// <summary>
    /// Sets up the query command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The query command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project.",
            Required = true
        };

        var questionOption = new Option<string>("--question", "-q")
        {
            Description = "The question to ask the model.",
            Required = true
        };

        var queryCommand = new Command("query-model", "Answer questions based on the semantic model by using Generative AI.");
        queryCommand.Options.Add(projectPathOption);
        queryCommand.Options.Add(questionOption);
        queryCommand.SetAction(async (parseResult) =>
        {
            var projectPath = parseResult.GetValue(projectPathOption)!;
            var question = parseResult.GetValue(questionOption)!;
            var handler = host.Services.GetRequiredService<QueryModelCommandHandler>();
            var options = new QueryModelCommandHandlerOptions(projectPath, question);
            await handler.HandleAsync(options);
        });

        return queryCommand;
    }

    /// <summary>
    /// Handles the query command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(QueryModelCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;
        var question = commandOptions.Question;

        if (string.IsNullOrWhiteSpace(question))
        {
            OutputStopError("Question must not be empty.");
            return;
        }

        _ = await LoadSemanticModelAsync(projectPath);

        _logger.LogInformation("{Message} '{ProjectPath}'",
            _resourceManagerLogMessages.GetString("QueryingProject"), projectPath.FullName);

        OutputInformation("Querying semantic model...\n");

        try
        {
            var request = new SemanticModelQueryRequest(question);
            var streamingResult = await _queryService.QueryStreamingAsync(request);

            await using (streamingResult.ConfigureAwait(false))
            {
                // Stream answer tokens to console
                await foreach (var token in streamingResult.Tokens)
                {
                    _outputService.Write(token);
                }

                _outputService.WriteLine("\n");

                // Display metadata after streaming completes
                var metadata = await streamingResult.GetMetadataAsync();

                if (metadata.ReferencedEntities.Count > 0)
                {
                    _outputService.WriteLine("Referenced Entities:");
                    foreach (var entity in metadata.ReferencedEntities)
                    {
                        _outputService.WriteLine($"  - [{entity.EntityType}] {entity.SchemaName}.{entity.EntityName} (score: {entity.Score:F2})");
                    }
                    _outputService.WriteLine("");
                }

                _outputService.WriteLine("Query Statistics:");
                _outputService.WriteLine($"  Response Rounds: {metadata.ResponseRounds}");
                _outputService.WriteLine($"  Tokens: {metadata.InputTokens:N0} input / {metadata.OutputTokens:N0} output / {metadata.TotalTokens:N0} total");
                _outputService.WriteLine($"  Duration: {metadata.Duration.TotalSeconds:F1}s");
                _outputService.WriteLine($"  Termination: {metadata.TerminationReason}");
            }
        }
        catch (InvalidOperationException ex)
        {
            OutputStopError(ex.Message);
        }
    }
}
