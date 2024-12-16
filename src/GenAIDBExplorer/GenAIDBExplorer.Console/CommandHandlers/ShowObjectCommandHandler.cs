using System.CommandLine;
using System.Resources;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using GenAIDBExplorer.Console.Services;

namespace GenAIDBExplorer.Console.CommandHandlers
{
    /// <summary>
    /// Command handler for showing details of database objects.
    /// </summary>
    /// <param name="project">The project instance to use.</param>
    /// <param name="semanticModelProvider">The semantic model provider instance.</param>
    /// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
    /// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
    public class ShowObjectCommandHandler(
        IProject project,
        ISemanticModelProvider semanticModelProvider,
        IDatabaseConnectionProvider connectionProvider,
        IOutputService outputService,
        IServiceProvider serviceProvider,
        ILogger<ICommandHandler<ShowObjectCommandHandlerOptions>> logger
    ) : CommandHandler<ShowObjectCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
    {
        private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(ShowObjectCommandHandler).Assembly);
        private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Console.Resources.ErrorMessages", typeof(ShowObjectCommandHandler).Assembly);

        /// <summary>
        /// Sets up the show command and its subcommands.
        /// </summary>
        /// <param name="host">The host instance.</param>
        /// <returns>The show command.</returns>
        public static Command SetupCommand(IHost host)
        {
            var projectPathOption = new Option<DirectoryInfo>(
                aliases: new[] { "--project", "-p" },
                description: "The path to the GenAI Database Explorer project."
            )
            {
                IsRequired = true
            };

            var schemaNameOption = new Option<string>(
                aliases: new[] { "--schemaName", "-s" },
                description: "The schema name of the object to show."
            )
            {
                IsRequired = true
            };

            var nameOption = new Option<string>(
                aliases: new[] { "--name", "-n" },
                description: "The name of the object to show."
            )
            {
                IsRequired = true
            };

            // Create the base 'show' command
            var showCommand = new Command("show-object", "Show details of a semantic model object.");

            // Create subcommands
            var tableCommand = new Command("table", "Show details of a table.")
            {
                projectPathOption,
                schemaNameOption,
                nameOption
            };
            tableCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name) =>
            {
                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "table");
                await handler.HandleAsync(options);
            }, projectPathOption, schemaNameOption, nameOption);

            var viewCommand = new Command("view", "Show details of a view.")
            {
                projectPathOption,
                schemaNameOption,
                nameOption
            };
            viewCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name) =>
            {
                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "view");
                await handler.HandleAsync(options);
            }, projectPathOption, schemaNameOption, nameOption);

            var storedProcedureCommand = new Command("storedprocedure", "Show details of a stored procedure.")
            {
                projectPathOption,
                schemaNameOption,
                nameOption
            };
            storedProcedureCommand.SetHandler(async (DirectoryInfo projectPath, string schemaName, string name) =>
            {
                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "storedprocedure");
                await handler.HandleAsync(options);
            }, projectPathOption, schemaNameOption, nameOption);

            // Add subcommands to the 'show' command
            showCommand.AddCommand(tableCommand);
            showCommand.AddCommand(viewCommand);
            showCommand.AddCommand(storedProcedureCommand);

            return showCommand;
        }

        /// <summary>
        /// Handles the show command with the specified options.
        /// </summary>
        /// <param name="commandOptions">The options for the command.</param>
        public override async Task HandleAsync(ShowObjectCommandHandlerOptions commandOptions)
        {
            AssertCommandOptionsValid(commandOptions);

            var projectPath = commandOptions.ProjectPath;
            var semanticModel = await LoadSemanticModelAsync(projectPath);

            // Show details based on object type
            switch (commandOptions.ObjectType.ToLower())
            {
                case "table":
                    await ShowTableDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                case "view":
                    await ShowViewDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                case "storedprocedure":
                    await ShowStoredProcedureDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                default:
                    var errorMessage = _resourceManagerErrorMessages.GetString("InvalidObjectType");
                    OutputStopError(errorMessage);
                    break;
            }
        }
    }
}