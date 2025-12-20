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
using System.Text.Json;

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
            var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
            {
                Description = "The path to the GenAI Database Explorer project.",
                Required = true
            };

            var schemaNameOption = new Option<string>("--schema-name", "-s")
            {
                Description = "The schema name of the object to show.",
                Required = true
            };

            var nameOption = new Option<string>("--name", "-n")
            {
                Description = "The name of the object to show.",
                Required = true
            };

            // Create the base 'show' command
            var showCommand = new Command("show-object", "Show details of a semantic model object.");

            // Create subcommands
            var tableCommand = new Command("table", "Show details of a table.");
            tableCommand.Options.Add(projectPathOption);
            tableCommand.Options.Add(schemaNameOption);
            tableCommand.Options.Add(nameOption);

            tableCommand.SetAction(async (parseResult) =>
            {
                var projectPath = parseResult.GetValue(projectPathOption)!;
                var schemaName = parseResult.GetValue(schemaNameOption)!;
                var name = parseResult.GetValue(nameOption)!;

                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "table");
                await handler.HandleAsync(options);
            });

            var viewCommand = new Command("view", "Show details of a view.");
            viewCommand.Options.Add(projectPathOption);
            viewCommand.Options.Add(schemaNameOption);
            viewCommand.Options.Add(nameOption);

            viewCommand.SetAction(async (parseResult) =>
            {
                var projectPath = parseResult.GetValue(projectPathOption)!;
                var schemaName = parseResult.GetValue(schemaNameOption)!;
                var name = parseResult.GetValue(nameOption)!;

                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "view");
                await handler.HandleAsync(options);
            });

            var storedProcedureCommand = new Command("storedprocedure", "Show details of a stored procedure.");
            storedProcedureCommand.Options.Add(projectPathOption);
            storedProcedureCommand.Options.Add(schemaNameOption);
            storedProcedureCommand.Options.Add(nameOption);

            storedProcedureCommand.SetAction(async (parseResult) =>
            {
                var projectPath = parseResult.GetValue(projectPathOption)!;
                var schemaName = parseResult.GetValue(schemaNameOption)!;
                var name = parseResult.GetValue(nameOption)!;

                var handler = host.Services.GetRequiredService<ShowObjectCommandHandler>();
                var options = new ShowObjectCommandHandlerOptions(projectPath, schemaName, name, "storedprocedure");
                await handler.HandleAsync(options);
            });

            // Add subcommands to the 'show' command
            showCommand.Subcommands.Add(tableCommand);
            showCommand.Subcommands.Add(viewCommand);
            showCommand.Subcommands.Add(storedProcedureCommand);

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
                    await PrintEmbeddingMetadataIfAvailableAsync(projectPath, "table", commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                case "view":
                    await ShowViewDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    await PrintEmbeddingMetadataIfAvailableAsync(projectPath, "view", commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                case "storedprocedure":
                    await ShowStoredProcedureDetailsAsync(semanticModel, commandOptions.SchemaName, commandOptions.ObjectName);
                    await PrintEmbeddingMetadataIfAvailableAsync(projectPath, "storedprocedure", commandOptions.SchemaName, commandOptions.ObjectName);
                    break;
                default:
                    var errorMessage = _resourceManagerErrorMessages.GetString("InvalidObjectType") ?? "Invalid object type specified.";
                    OutputStopError(errorMessage);
                    break;
            }
        }

        /// <summary>
        /// Attempts to read and print embedding metadata for the specified entity if present in the persisted envelope.
        /// The metadata is persisted for LocalDisk/AzureBlob strategies under the semantic model repository directory.
        /// </summary>
        private async Task PrintEmbeddingMetadataIfAvailableAsync(DirectoryInfo projectPath, string objectType, string schemaName, string objectName)
        {
            FileInfo? entityFile = null;
            try
            {
                // Resolve the configured semantic model directory (LocalDisk only). If unsupported, skip gracefully.
                var modelDir = GetSemanticModelDirectory(projectPath);
                var folderName = objectType.ToLower() switch
                {
                    "table" => "tables",
                    "view" => "views",
                    "storedprocedure" => "storedprocedures",
                    _ => null
                };
                if (folderName is null) return;

                var fileName = $"{schemaName}.{objectName}.json";
                var preferred = new FileInfo(Path.Combine(modelDir.FullName, folderName, fileName));
                if (preferred.Exists)
                {
                    entityFile = preferred;
                }
                else
                {
                    // Probe common alternate roots used by older versions or different casing
                    var alt1 = new FileInfo(Path.Combine(projectPath.FullName, "semantic-model", folderName, fileName));
                    var alt2 = new FileInfo(Path.Combine(projectPath.FullName, "SemanticModel", folderName, fileName));
                    entityFile = alt1.Exists ? alt1 : (alt2.Exists ? alt2 : preferred);
                    if (!entityFile.Exists) return; // Nothing to show
                }
            }
            catch
            {
                // If the persistence strategy isn't LocalDisk (throws) or path cannot be resolved, just skip output
                return;
            }

            try
            {
                var raw = await File.ReadAllTextAsync(entityFile.FullName).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                // Try envelope shape: { data: { ... }, embedding: { metadata: { ... } } }
                if (TryGetPropertyIgnoreCase(doc.RootElement, "embedding", out var emb))
                {
                    JsonElement mdCandidate = emb;
                    if (TryGetPropertyIgnoreCase(emb, "metadata", out var md))
                    {
                        mdCandidate = md;
                    }

                    if (TryExtractMetadata(mdCandidate, out var lines))
                    {
                        OutputInformation("");
                        OutputInformation("Embedding Metadata:");
                        foreach (var line in lines) OutputInformation(line);
                        return;
                    }
                }

                // Fallback: direct metadata property at root (less common)
                if (TryGetPropertyIgnoreCase(doc.RootElement, "metadata", out var mdRoot))
                {
                    if (TryExtractMetadata(mdRoot, out var lines))
                    {
                        OutputInformation("");
                        OutputInformation("Embedding Metadata:");
                        foreach (var line in lines) OutputInformation(line);
                    }
                }
            }
            catch
            {
                // Non-fatal: best-effort only
            }
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            value = default;
            if (element.ValueKind != JsonValueKind.Object) return false;
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Extracts known embedding metadata fields from the provided JSON element.
        /// </summary>
        private static bool TryExtractMetadata(JsonElement element, out List<string> lines)
        {
            lines = new List<string>();
            if (element.ValueKind != JsonValueKind.Object) return false;

            static string? GetString(JsonElement obj, string name)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                    {
                        return p.Value.GetString();
                    }
                }
                return null;
            }

            static int? GetInt(JsonElement obj, string name)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var i)) return i;
                        if (p.Value.ValueKind == JsonValueKind.String && int.TryParse(p.Value.GetString(), out var j)) return j;
                    }
                }
                return null;
            }

            var modelId = GetString(element, "modelId");
            var dimensions = GetInt(element, "dimensions");
            var contentHash = GetString(element, "contentHash");
            var generatedAt = GetString(element, "generatedAt");
            var serviceId = GetString(element, "serviceId");
            var version = GetString(element, "version");

            if (modelId is null && dimensions is null && contentHash is null && generatedAt is null && serviceId is null && version is null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(modelId)) lines.Add($"  ModelId: {modelId}");
            if (dimensions is not null) lines.Add($"  Dimensions: {dimensions}");
            if (!string.IsNullOrWhiteSpace(serviceId)) lines.Add($"  ServiceId: {serviceId}");
            if (!string.IsNullOrWhiteSpace(contentHash)) lines.Add($"  ContentHash: {contentHash}");
            if (!string.IsNullOrWhiteSpace(generatedAt)) lines.Add($"  GeneratedAt: {generatedAt}");
            if (!string.IsNullOrWhiteSpace(version)) lines.Add($"  Version: {version}");
            return lines.Count > 0;
        }
    }
}