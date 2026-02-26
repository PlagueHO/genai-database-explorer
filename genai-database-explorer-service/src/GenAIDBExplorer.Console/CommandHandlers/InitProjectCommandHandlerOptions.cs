namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Represents the options for the Init command handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitProjectCommandHandlerOptions"/> class.
/// </remarks>
/// <param name="projectPath">The path to the project directory.</param>
/// <param name="databaseName">Optional database display name.</param>
/// <param name="databaseConnectionString">Optional database connection string.</param>
/// <param name="databaseAuthType">Optional database authentication type.</param>
/// <param name="databaseSchema">Optional database schema filter.</param>
/// <param name="foundryEndpoint">Optional Foundry Models endpoint URL.</param>
/// <param name="foundryAuthType">Optional Foundry Models authentication type.</param>
/// <param name="foundryApiKey">Optional Foundry Models API key.</param>
/// <param name="foundryChatDeployment">Optional chat completion deployment name.</param>
/// <param name="foundryEmbeddingDeployment">Optional embedding deployment name.</param>
/// <param name="persistenceStrategy">Optional semantic model persistence strategy.</param>
/// <param name="vectorIndexProvider">Optional vector index provider.</param>
/// <param name="vectorIndexCollectionName">Optional vector index collection name.</param>
public class InitProjectCommandHandlerOptions(
    DirectoryInfo projectPath,
    string? databaseName = null,
    string? databaseConnectionString = null,
    string? databaseAuthType = null,
    string? databaseSchema = null,
    string? foundryEndpoint = null,
    string? foundryAuthType = null,
    string? foundryApiKey = null,
    string? foundryChatDeployment = null,
    string? foundryEmbeddingDeployment = null,
    string? persistenceStrategy = null,
    string? vectorIndexProvider = null,
    string? vectorIndexCollectionName = null
) : CommandHandlerOptions(projectPath)
{
    /// <summary>Gets the optional database display name.</summary>
    public string? DatabaseName { get; } = databaseName;

    /// <summary>Gets the optional database connection string.</summary>
    public string? DatabaseConnectionString { get; } = databaseConnectionString;

    /// <summary>Gets the optional database authentication type (SqlAuthentication or EntraIdAuthentication).</summary>
    public string? DatabaseAuthType { get; } = databaseAuthType;

    /// <summary>Gets the optional database schema filter.</summary>
    public string? DatabaseSchema { get; } = databaseSchema;

    /// <summary>Gets the optional Foundry Models endpoint URL.</summary>
    public string? FoundryEndpoint { get; } = foundryEndpoint;

    /// <summary>Gets the optional Foundry Models authentication type (EntraIdAuthentication or ApiKey).</summary>
    public string? FoundryAuthType { get; } = foundryAuthType;

    /// <summary>Gets the optional Foundry Models API key.</summary>
    public string? FoundryApiKey { get; } = foundryApiKey;

    /// <summary>Gets the optional chat completion deployment name.</summary>
    public string? FoundryChatDeployment { get; } = foundryChatDeployment;

    /// <summary>Gets the optional embedding deployment name.</summary>
    public string? FoundryEmbeddingDeployment { get; } = foundryEmbeddingDeployment;

    /// <summary>Gets the optional semantic model persistence strategy (LocalDisk, AzureBlob, or CosmosDB).</summary>
    public string? PersistenceStrategy { get; } = persistenceStrategy;

    /// <summary>Gets the optional vector index provider (Auto, AzureAISearch, CosmosDB, or InMemory).</summary>
    public string? VectorIndexProvider { get; } = vectorIndexProvider;

    /// <summary>Gets the optional vector index collection name.</summary>
    public string? VectorIndexCollectionName { get; } = vectorIndexCollectionName;

    /// <summary>
    /// Returns true if any settings overrides were provided.
    /// </summary>
    public bool HasSettingsOverrides =>
        DatabaseName is not null ||
        DatabaseConnectionString is not null ||
        DatabaseAuthType is not null ||
        DatabaseSchema is not null ||
        FoundryEndpoint is not null ||
        FoundryAuthType is not null ||
        FoundryApiKey is not null ||
        FoundryChatDeployment is not null ||
        FoundryEmbeddingDeployment is not null ||
        PersistenceStrategy is not null ||
        VectorIndexProvider is not null ||
        VectorIndexCollectionName is not null;
}
