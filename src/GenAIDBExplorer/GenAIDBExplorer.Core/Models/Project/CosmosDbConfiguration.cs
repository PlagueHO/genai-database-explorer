using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Configuration options for Cosmos DB persistence strategy.
/// </summary>
public sealed class CosmosDbConfiguration
{
    /// <summary>
    /// Configuration section name for Cosmos DB settings.
    /// </summary>
    public const string SectionName = "SemanticModelRepository:CosmosDb";

    /// <summary>
    /// Gets or sets the Cosmos DB account endpoint URI.
    /// Example: https://mycosmosaccount.documents.azure.com:443/
    /// </summary>
    [Required]
    [Url]
    public required string AccountEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the Cosmos DB database.
    /// Default is "SemanticModels".
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$",
        ErrorMessage = "Database name must be 1-255 characters, alphanumeric with underscores, hyphens, and periods")]
    public string DatabaseName { get; set; } = "SemanticModels";

    /// <summary>
    /// Gets or sets the name of the container for semantic model metadata.
    /// Default is "Models".
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$",
        ErrorMessage = "Container name must be 1-255 characters, alphanumeric with underscores, hyphens, and periods")]
    public string ModelsContainerName { get; set; } = "Models";

    /// <summary>
    /// Gets or sets the name of the container for semantic model entities (tables, views, stored procedures).
    /// Default is "ModelEntities".
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$",
        ErrorMessage = "Container name must be 1-255 characters, alphanumeric with underscores, hyphens, and periods")]
    public string EntitiesContainerName { get; set; } = "ModelEntities";

    /// <summary>
    /// Gets or sets the partition key path for the models container.
    /// Default is "/modelName" for even distribution by model name.
    /// </summary>
    [Required]
    public string ModelsPartitionKeyPath { get; set; } = "/modelName";

    /// <summary>
    /// Gets or sets the partition key path for the entities container.
    /// Default is "/modelName" to co-locate entities with their parent model.
    /// </summary>
    [Required]
    public string EntitiesPartitionKeyPath { get; set; } = "/modelName";

    /// <summary>
    /// Gets or sets the provisioned throughput for the database (RU/s).
    /// Default is 400 RU/s. Set to null for serverless.
    /// </summary>
    [Range(400, 1000000)]
    public int? DatabaseThroughput { get; set; } = 400;

    /// <summary>
    /// Gets or sets the timeout for Cosmos DB operations in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    [Range(30, 3600)]
    public int OperationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum concurrent operations for batch operations.
    /// Default is 4.
    /// </summary>
    [Range(1, 16)]
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// Gets or sets the maximum retry attempts for transient failures.
    /// Default is 3.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the consistency level for read operations.
    /// Default is Session for optimal balance of consistency and performance.
    /// </summary>
    public CosmosConsistencyLevel ConsistencyLevel { get; set; } = CosmosConsistencyLevel.Session;
}

/// <summary>
/// Cosmos DB consistency levels.
/// </summary>
public enum CosmosConsistencyLevel
{
    /// <summary>
    /// Eventual consistency - highest performance, lowest consistency.
    /// </summary>
    Eventual,

    /// <summary>
    /// Consistent prefix - reads see writes in order within a partition.
    /// </summary>
    ConsistentPrefix,

    /// <summary>
    /// Session consistency - consistent within a session (default).
    /// </summary>
    Session,

    /// <summary>
    /// Bounded staleness - guaranteed consistency within time/operations bounds.
    /// </summary>
    BoundedStaleness,

    /// <summary>
    /// Strong consistency - highest consistency, lowest performance.
    /// </summary>
    Strong
}
