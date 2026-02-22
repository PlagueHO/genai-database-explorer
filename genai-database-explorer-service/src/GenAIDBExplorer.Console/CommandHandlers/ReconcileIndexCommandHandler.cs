using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticVectors.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace GenAIDBExplorer.Console.CommandHandlers;

public sealed class ReconcileIndexCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<ReconcileIndexCommandHandlerOptions>> logger
) : CommandHandler<ReconcileIndexCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project",
            Required = true
        };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Run without writing files or index" };

        var cmd = new Command("reconcile-index", "Reconcile vector index with local persisted embeddings.");
        cmd.Options.Add(projectPathOption);
        cmd.Options.Add(dryRunOption);

        cmd.SetAction(async pr =>
        {
            var handler = host.Services.GetRequiredService<ReconcileIndexCommandHandler>();
            var opt = new ReconcileIndexCommandHandlerOptions(
                pr.GetValue(projectPathOption)!,
                pr.GetValue(dryRunOption));
            await handler.HandleAsync(opt);
        });

        return cmd;
    }

    public override async Task HandleAsync(ReconcileIndexCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);
        var model = await LoadSemanticModelAsync(commandOptions.ProjectPath);

        // For Phase 4: simple re-index by calling generation with Overwrite=true or DryRun
        var orchestrator = _serviceProvider.GetRequiredService<IVectorOrchestrator>();
        var options = new VectorGenerationOptions
        {
            Overwrite = true,
            DryRun = commandOptions.DryRun,
            SkipTables = false,
            SkipViews = false,
            SkipStoredProcedures = false
        };
        var processed = await orchestrator.GenerateAsync(model, commandOptions.ProjectPath, options);
        _outputService.WriteLine($"Reconciled {processed} entities.");
    }
}
