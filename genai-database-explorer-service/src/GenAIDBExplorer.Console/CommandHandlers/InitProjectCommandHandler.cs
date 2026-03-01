using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for initializing a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitProjectCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to initialize.</param>
/// <param name="connectionProvider">The database connection provider instance for connecting to a SQL database.</param>
/// <param name="semanticModelProvider">The semantic model provider instance for building a semantic model of the database.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class InitProjectCommandHandler(
    IProject project,
    ISemanticModelProvider semanticModelProvider,
    IDatabaseConnectionProvider connectionProvider,
    IOutputService outputService,
    IServiceProvider serviceProvider,
    ILogger<ICommandHandler<InitProjectCommandHandlerOptions>> logger
) : CommandHandler<InitProjectCommandHandlerOptions>(project, connectionProvider, semanticModelProvider, outputService, serviceProvider, logger)
{
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Console.Resources.LogMessages", typeof(InitProjectCommandHandler).Assembly);

    private static readonly string[] ValidDatabaseAuthTypes = ["SqlAuthentication", "EntraIdAuthentication"];
    private static readonly string[] ValidFoundryAuthTypes = ["EntraIdAuthentication", "ApiKey"];
    private static readonly string[] ValidPersistenceStrategies = ["LocalDisk", "AzureBlob", "CosmosDB"];
    private static readonly string[] ValidVectorIndexProviders = ["Auto", "AzureAISearch", "CosmosDB", "InMemory"];

    /// <summary>
    /// Sets up the init-project command.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <returns>The init-project command.</returns>
    public static Command SetupCommand(IHost host)
    {
        var projectPathOption = new Option<DirectoryInfo>("--project", "-p")
        {
            Description = "The path to the GenAI Database Explorer project.",
            Required = true
        };

        // Database settings options
        var databaseNameOption = new Option<string?>("--database-name")
        {
            Description = "The display name of the database."
        };

        var databaseConnectionStringOption = new Option<string?>("--database-connection-string")
        {
            Description = "The SQL Server connection string for the database."
        };

        var databaseAuthTypeOption = new Option<string?>("--database-auth-type")
        {
            Description = "The database authentication type (SqlAuthentication or EntraIdAuthentication)."
        };

        var databaseSchemaOption = new Option<string?>("--database-schema")
        {
            Description = "The database schema to extract. If omitted, all schemas will be extracted."
        };

        // Foundry Models settings options
        var foundryEndpointOption = new Option<string?>("--foundry-endpoint")
        {
            Description = "The Microsoft Foundry project endpoint URL (e.g., https://<resource>.services.ai.azure.com/api/projects/<project-name>)."
        };

        var foundryAuthTypeOption = new Option<string?>("--foundry-auth-type")
        {
            Description = "The Microsoft Foundry authentication type (EntraIdAuthentication or ApiKey)."
        };

        var foundryApiKeyOption = new Option<string?>("--foundry-api-key")
        {
            Description = "The Microsoft Foundry API key (required when using ApiKey authentication)."
        };

        var foundryChatDeploymentOption = new Option<string?>("--foundry-chat-deployment")
        {
            Description = "The chat completion model deployment name."
        };

        var foundryEmbeddingDeploymentOption = new Option<string?>("--foundry-embedding-deployment")
        {
            Description = "The embedding model deployment name."
        };

        // Semantic model settings options
        var persistenceStrategyOption = new Option<string?>("--persistence-strategy")
        {
            Description = "The semantic model persistence strategy (LocalDisk, AzureBlob, or CosmosDB)."
        };

        // Vector index settings options
        var vectorIndexProviderOption = new Option<string?>("--vector-index-provider")
        {
            Description = "The vector index provider (Auto, AzureAISearch, CosmosDB, or InMemory)."
        };

        var vectorIndexCollectionNameOption = new Option<string?>("--vector-index-collection-name")
        {
            Description = "The vector index collection name."
        };

        var initCommand = new Command("init-project", "Initialize a GenAI Database Explorer project.");
        initCommand.Options.Add(projectPathOption);
        initCommand.Options.Add(databaseNameOption);
        initCommand.Options.Add(databaseConnectionStringOption);
        initCommand.Options.Add(databaseAuthTypeOption);
        initCommand.Options.Add(databaseSchemaOption);
        initCommand.Options.Add(foundryEndpointOption);
        initCommand.Options.Add(foundryAuthTypeOption);
        initCommand.Options.Add(foundryApiKeyOption);
        initCommand.Options.Add(foundryChatDeploymentOption);
        initCommand.Options.Add(foundryEmbeddingDeploymentOption);
        initCommand.Options.Add(persistenceStrategyOption);
        initCommand.Options.Add(vectorIndexProviderOption);
        initCommand.Options.Add(vectorIndexCollectionNameOption);

        initCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var projectPath = parseResult.GetValue(projectPathOption)!;
            var handler = host.Services.GetRequiredService<InitProjectCommandHandler>();
            var options = new InitProjectCommandHandlerOptions(
                projectPath,
                databaseName: parseResult.GetValue(databaseNameOption),
                databaseConnectionString: parseResult.GetValue(databaseConnectionStringOption),
                databaseAuthType: parseResult.GetValue(databaseAuthTypeOption),
                databaseSchema: parseResult.GetValue(databaseSchemaOption),
                foundryEndpoint: parseResult.GetValue(foundryEndpointOption),
                foundryAuthType: parseResult.GetValue(foundryAuthTypeOption),
                foundryApiKey: parseResult.GetValue(foundryApiKeyOption),
                foundryChatDeployment: parseResult.GetValue(foundryChatDeploymentOption),
                foundryEmbeddingDeployment: parseResult.GetValue(foundryEmbeddingDeploymentOption),
                persistenceStrategy: parseResult.GetValue(persistenceStrategyOption),
                vectorIndexProvider: parseResult.GetValue(vectorIndexProviderOption),
                vectorIndexCollectionName: parseResult.GetValue(vectorIndexCollectionNameOption)
            );
            return await handler.HandleWithExitCodeAsync(options);
        });

        return initCommand;
    }

    /// <summary>
    /// Handles the initialization command with the specified project path.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    public override async Task HandleAsync(InitProjectCommandHandlerOptions commandOptions)
    {
        await HandleWithExitCodeAsync(commandOptions);
    }

    /// <summary>
    /// Handles the initialization command and returns an exit code.
    /// </summary>
    /// <param name="commandOptions">The options for the command.</param>
    /// <returns>0 on success, 1 on failure.</returns>
    internal async Task<int> HandleWithExitCodeAsync(InitProjectCommandHandlerOptions commandOptions)
    {
        AssertCommandOptionsValid(commandOptions);

        var projectPath = commandOptions.ProjectPath;

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("InitializingProject"), projectPath.FullName);

        ValidateProjectPath(projectPath);

        // Validate settings overrides before initializing
        if (commandOptions.HasSettingsOverrides)
        {
            try
            {
                ValidateSettingsOverrides(commandOptions);
            }
            catch (ArgumentException ex)
            {
                OutputStopError(ex.Message);
                return 1;
            }
        }

        // Initialize the project directory, but catch the exception if the directory is not empty
        try
        {
            _project.InitializeProjectDirectory(projectPath);
        }
        catch (Exception ex)
        {
            OutputStopError(ex.Message);
            return 1;
        }

        // Apply settings overrides if any were provided
        if (commandOptions.HasSettingsOverrides)
        {
            try
            {
                ApplySettingsOverrides(projectPath, commandOptions);
                _logger.LogInformation("Project settings updated with provided configuration values.");
            }
            catch (Exception ex)
            {
                OutputStopError($"Failed to apply settings overrides: {ex.Message}");
                return 1;
            }
        }

        _logger.LogInformation("{Message} '{ProjectPath}'", _resourceManagerLogMessages.GetString("InitializeProjectComplete"), projectPath.FullName);
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// Validates the settings override values provided in command options.
    /// </summary>
    /// <param name="options">The command options containing settings overrides.</param>
    /// <exception cref="ArgumentException">Thrown when a settings value is invalid.</exception>
    internal static void ValidateSettingsOverrides(InitProjectCommandHandlerOptions options)
    {
        if (options.DatabaseAuthType is not null &&
            !ValidDatabaseAuthTypes.Contains(options.DatabaseAuthType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Invalid database authentication type '{options.DatabaseAuthType}'. Valid values are: {string.Join(", ", ValidDatabaseAuthTypes)}.");
        }

        if (options.FoundryEndpoint is not null)
        {
            if (!Uri.TryCreate(options.FoundryEndpoint, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    $"Foundry endpoint '{options.FoundryEndpoint}' must be a valid HTTPS URL.");
            }
        }

        if (options.FoundryAuthType is not null &&
            !ValidFoundryAuthTypes.Contains(options.FoundryAuthType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Invalid Foundry authentication type '{options.FoundryAuthType}'. Valid values are: {string.Join(", ", ValidFoundryAuthTypes)}.");
        }

        if (options.PersistenceStrategy is not null &&
            !ValidPersistenceStrategies.Contains(options.PersistenceStrategy, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Invalid persistence strategy '{options.PersistenceStrategy}'. Valid values are: {string.Join(", ", ValidPersistenceStrategies)}.");
        }

        if (options.VectorIndexProvider is not null &&
            !ValidVectorIndexProviders.Contains(options.VectorIndexProvider, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Invalid vector index provider '{options.VectorIndexProvider}'. Valid values are: {string.Join(", ", ValidVectorIndexProviders)}.");
        }
    }

    /// <summary>
    /// Applies settings overrides to the settings.json file in the project directory.
    /// </summary>
    /// <param name="projectPath">The project directory containing settings.json.</param>
    /// <param name="options">The command options containing settings overrides.</param>
    internal static void ApplySettingsOverrides(DirectoryInfo projectPath, InitProjectCommandHandlerOptions options)
    {
        var settingsPath = Path.Combine(projectPath.FullName, "settings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("settings.json not found in project directory.", settingsPath);
        }

        var jsonText = File.ReadAllText(settingsPath);
        var jsonNode = JsonNode.Parse(jsonText, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })
            ?? throw new InvalidOperationException("Failed to parse settings.json.");

        // Apply Database settings
        if (options.DatabaseName is not null)
        {
            EnsureJsonObject(jsonNode, "Database")["Name"] = options.DatabaseName;
        }

        if (options.DatabaseConnectionString is not null)
        {
            EnsureJsonObject(jsonNode, "Database")["ConnectionString"] = options.DatabaseConnectionString;
        }

        if (options.DatabaseAuthType is not null)
        {
            EnsureJsonObject(jsonNode, "Database")["AuthenticationType"] = options.DatabaseAuthType;
        }

        if (options.DatabaseSchema is not null)
        {
            EnsureJsonObject(jsonNode, "Database")["Schema"] = options.DatabaseSchema;
        }

        // Apply MicrosoftFoundry settings
        if (options.FoundryEndpoint is not null)
        {
            EnsureJsonObject(EnsureJsonObject(jsonNode, "MicrosoftFoundry"), "Default")["Endpoint"] = options.FoundryEndpoint;
        }

        if (options.FoundryAuthType is not null)
        {
            EnsureJsonObject(EnsureJsonObject(jsonNode, "MicrosoftFoundry"), "Default")["AuthenticationType"] = options.FoundryAuthType;
        }

        if (options.FoundryApiKey is not null)
        {
            EnsureJsonObject(EnsureJsonObject(jsonNode, "MicrosoftFoundry"), "Default")["ApiKey"] = options.FoundryApiKey;
        }

        if (options.FoundryChatDeployment is not null)
        {
            EnsureJsonObject(EnsureJsonObject(jsonNode, "MicrosoftFoundry"), "ChatCompletion")["DeploymentName"] = options.FoundryChatDeployment;
        }

        if (options.FoundryEmbeddingDeployment is not null)
        {
            EnsureJsonObject(EnsureJsonObject(jsonNode, "MicrosoftFoundry"), "Embedding")["DeploymentName"] = options.FoundryEmbeddingDeployment;
        }

        // Apply SemanticModel settings
        if (options.PersistenceStrategy is not null)
        {
            EnsureJsonObject(jsonNode, "SemanticModel")["PersistenceStrategy"] = options.PersistenceStrategy;
        }

        // Apply VectorIndex settings
        if (options.VectorIndexProvider is not null)
        {
            EnsureJsonObject(jsonNode, "VectorIndex")["Provider"] = options.VectorIndexProvider;
        }

        if (options.VectorIndexCollectionName is not null)
        {
            EnsureJsonObject(jsonNode, "VectorIndex")["CollectionName"] = options.VectorIndexCollectionName;
        }

        // Write back with indented formatting
        var writerOptions = new JsonWriterOptions { Indented = true };
        using var stream = File.Create(settingsPath);
        using var writer = new Utf8JsonWriter(stream, writerOptions);
        jsonNode.WriteTo(writer);
    }

    /// <summary>
    /// Ensures a JSON object property exists on the parent node, creating it if needed.
    /// </summary>
    private static JsonObject EnsureJsonObject(JsonNode parent, string propertyName)
    {
        if (parent is JsonObject parentObj)
        {
            if (parentObj[propertyName] is not JsonObject existing)
            {
                existing = new JsonObject();
                parentObj[propertyName] = existing;
            }
            return existing;
        }

        throw new InvalidOperationException($"Cannot access property '{propertyName}' on a non-object JSON node.");
    }
}
