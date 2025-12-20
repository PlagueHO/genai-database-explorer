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

public sealed class GenerateVectorsCommandHandler(
    IProject project,
    IDatabaseConnectionProvider connectionProvider,
    ISemanticModelProvider semanticModelProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<GenerateVectorsCommandHandlerOptions>> logger
) : CommandHandler<GenerateVectorsCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project",
            Required = true
        };
        var overwriteOption = new Option<bool>("--overwrite") { Description = "Regenerate even if content hash hasn't changed" };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Run without writing files or index" };
        var skipTablesOption = new Option<bool>("--skip-tables") { Description = "Skip tables" };
        var skipViewsOption = new Option<bool>("--skip-views") { Description = "Skip views" };
        var skipStoredProceduresOption = new Option<bool>("--skip-stored-procedures") { Description = "Skip stored procedures" };
        var schemaOption = new Option<string>("--schema-name", "-s") { Description = "Schema name for a specific object" };
        var nameOption = new Option<string>("--name", "-n") { Description = "Object name for a specific object" };

        var cmd = new Command("generate-vectors", "Generate and index embeddings for entities.");
        cmd.Options.Add(projectPathOption);
        cmd.Options.Add(overwriteOption);
        cmd.Options.Add(dryRunOption);
        cmd.Options.Add(skipTablesOption);
        cmd.Options.Add(skipViewsOption);
        cmd.Options.Add(skipStoredProceduresOption);

        // Subcommands for specific types
        var table = new Command("table", "Generate for a specific table");
        table.Options.Add(projectPathOption);
        table.Options.Add(schemaOption);
        table.Options.Add(nameOption);
        table.Options.Add(overwriteOption);
        table.Options.Add(dryRunOption);
        table.SetAction(async pr =>
        {
            var handler = host.Services.GetRequiredService<GenerateVectorsCommandHandler>();
            var opt = new GenerateVectorsCommandHandlerOptions(
                pr.GetValue(projectPathOption)!,
                pr.GetValue(overwriteOption),
                pr.GetValue(dryRunOption),
                skipTables: false, skipViews: true, skipStoredProcedures: true,
                objectType: "table",
                schemaName: pr.GetValue(schemaOption),
                objectName: pr.GetValue(nameOption));
            await handler.HandleAsync(opt);
        });
        cmd.Subcommands.Add(table);

        var view = new Command("view", "Generate for a specific view");
        view.Options.Add(projectPathOption);
        view.Options.Add(schemaOption);
        view.Options.Add(nameOption);
        view.Options.Add(overwriteOption);
        view.Options.Add(dryRunOption);
        view.SetAction(async pr =>
        {
            var handler = host.Services.GetRequiredService<GenerateVectorsCommandHandler>();
            var opt = new GenerateVectorsCommandHandlerOptions(
                pr.GetValue(projectPathOption)!,
                pr.GetValue(overwriteOption),
                pr.GetValue(dryRunOption),
                skipTables: true, skipViews: false, skipStoredProcedures: true,
                objectType: "view",
                schemaName: pr.GetValue(schemaOption),
                objectName: pr.GetValue(nameOption));
            await handler.HandleAsync(opt);
        });
        cmd.Subcommands.Add(view);

        var sp = new Command("storedprocedure", "Generate for a specific stored procedure");
        sp.Options.Add(projectPathOption);
        sp.Options.Add(schemaOption);
        sp.Options.Add(nameOption);
        sp.Options.Add(overwriteOption);
        sp.Options.Add(dryRunOption);
        sp.SetAction(async pr =>
        {
            var handler = host.Services.GetRequiredService<GenerateVectorsCommandHandler>();
            var opt = new GenerateVectorsCommandHandlerOptions(
                pr.GetValue(projectPathOption)!,
                pr.GetValue(overwriteOption),
                pr.GetValue(dryRunOption),
                skipTables: true, skipViews: true, skipStoredProcedures: false,
                objectType: "storedprocedure",
                schemaName: pr.GetValue(schemaOption),
                objectName: pr.GetValue(nameOption));
            await handler.HandleAsync(opt);
        });
        cmd.Subcommands.Add(sp);

        cmd.SetAction(async pr =>
        {
            var handler = host.Services.GetRequiredService<GenerateVectorsCommandHandler>();
            var opt = new GenerateVectorsCommandHandlerOptions(
                pr.GetValue(projectPathOption)!,
                pr.GetValue(overwriteOption),
                pr.GetValue(dryRunOption),
                pr.GetValue(skipTablesOption),
                pr.GetValue(skipViewsOption),
                pr.GetValue(skipStoredProceduresOption));
            await handler.HandleAsync(opt);
        });

        return cmd;
    }

    public override async Task HandleAsync(GenerateVectorsCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);
        var model = await LoadSemanticModelAsync(commandOptions.ProjectPath);

        var orchestrator = _serviceProvider.GetRequiredService<IVectorOrchestrator>();
        var options = new VectorGenerationOptions
        {
            Overwrite = commandOptions.Overwrite,
            DryRun = commandOptions.DryRun,
            SkipTables = commandOptions.SkipTables,
            SkipViews = commandOptions.SkipViews,
            SkipStoredProcedures = commandOptions.SkipStoredProcedures,
            ObjectType = commandOptions.ObjectType,
            SchemaName = commandOptions.SchemaName,
            ObjectName = commandOptions.ObjectName
        };

        var processed = await orchestrator.GenerateAsync(model, commandOptions.ProjectPath, options);
        _outputService.WriteLine($"Processed {processed} entities.");
    }
}
