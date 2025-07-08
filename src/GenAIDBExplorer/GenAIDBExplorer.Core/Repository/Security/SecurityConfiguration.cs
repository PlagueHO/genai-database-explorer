namespace GenAIDBExplorer.Core.Repository.Security;

/// <summary>
/// Configuration options for secure JSON serialization operations.
/// </summary>
/// <remarks>
/// This configuration class provides fine-grained control over security features
/// in JSON serialization and deserialization operations. It allows applications
/// to customize security thresholds based on their specific requirements while
/// maintaining secure defaults for enterprise deployment scenarios.
/// </remarks>
public sealed class SecureJsonSerializerOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed JSON size in bytes. Default is 50MB.
    /// </summary>
    /// <remarks>
    /// This limit helps prevent denial-of-service attacks through oversized JSON payloads.
    /// Adjust based on your application's legitimate JSON size requirements.
    /// </remarks>
    public int MaxJsonSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Gets or sets the maximum allowed JSON nesting depth. Default is 64.
    /// </summary>
    /// <remarks>
    /// This limit prevents stack overflow attacks through deeply nested JSON structures.
    /// Most legitimate use cases don't require more than 10-20 levels of nesting.
    /// </remarks>
    public int MaxJsonDepth { get; set; } = 64;

    /// <summary>
    /// Gets or sets the maximum allowed string length in JSON. Default is 1MB.
    /// </summary>
    /// <remarks>
    /// This limit prevents memory exhaustion attacks through extremely long strings.
    /// Adjust based on your application's legitimate string length requirements.
    /// </remarks>
    public int MaxStringLength { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Gets or sets the maximum allowed array length in JSON. Default is 100,000.
    /// </summary>
    /// <remarks>
    /// This limit prevents performance degradation through extremely large arrays.
    /// Adjust based on your application's legitimate array size requirements.
    /// </remarks>
    public int MaxArrayLength { get; set; } = 100000;

    /// <summary>
    /// Gets or sets whether to enable strict pattern validation. Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the serializer will reject JSON containing potentially dangerous
    /// patterns such as script tags, JavaScript protocols, and injection attempts.
    /// Disable only if you need to process legitimate content that might match these patterns.
    /// </remarks>
    public bool EnableStrictPatternValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable audit logging for serialization operations. Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, all serialization operations will be logged for security monitoring
    /// and compliance purposes. This is recommended for production environments.
    /// </remarks>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to normalize Unicode characters for security. Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, Unicode characters will be normalized to prevent homograph attacks
    /// and other Unicode-based security vulnerabilities. This is recommended for production.
    /// </remarks>
    public bool EnableUnicodeNormalization { get; set; } = true;

    /// <summary>
    /// Gets or sets additional dangerous patterns to check for during validation.
    /// </summary>
    /// <remarks>
    /// Use this property to add application-specific patterns that should be considered
    /// dangerous in your context. Patterns should be valid regular expressions.
    /// </remarks>
    public string[] AdditionalDangerousPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to allow data URI schemes in JSON content. Default is false.
    /// </summary>
    /// <remarks>
    /// Data URIs can be used for legitimate purposes (like embedded images) but can also
    /// be used for attacks. Enable only if your application requires data URI support.
    /// </remarks>
    public bool AllowDataUriSchemes { get; set; } = false;

    /// <summary>
    /// Gets or sets the timeout for JSON processing operations in milliseconds. Default is 30 seconds.
    /// </summary>
    /// <remarks>
    /// This timeout prevents denial-of-service attacks through JSON that takes extremely
    /// long to process. Adjust based on your application's performance requirements.
    /// </remarks>
    public int ProcessingTimeoutMs { get; set; } = 30000; // 30 seconds
}

/// <summary>
/// Configuration options for Azure Key Vault integration.
/// </summary>
/// <remarks>
/// This configuration class provides comprehensive settings for Azure Key Vault
/// integration including authentication options, caching behavior, fallback
/// mechanisms, and security features for enterprise deployment scenarios.
/// </remarks>
public sealed class KeyVaultOptions
{
    /// <summary>
    /// Gets or sets the Azure Key Vault URI. This is required for Key Vault operations.
    /// </summary>
    /// <remarks>
    /// The URI should be in the format: https://{vault-name}.vault.azure.net/
    /// This can be retrieved from the Azure portal or Azure CLI.
    /// </remarks>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets whether to enable Key Vault integration. Default is false.
    /// </summary>
    /// <remarks>
    /// When disabled, the application will fall back to environment variables and
    /// default values for configuration retrieval. This is useful for development
    /// environments or when Key Vault is not available.
    /// </remarks>
    public bool EnableKeyVault { get; set; } = false;

    /// <summary>
    /// Gets or sets the cache expiration time for retrieved secrets. Default is 30 minutes.
    /// </summary>
    /// <remarks>
    /// Shorter expiration times provide better security by refreshing secrets more
    /// frequently, but may impact performance. Longer times improve performance
    /// but may expose applications to longer periods with potentially stale secrets.
    /// </remarks>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the timeout for Key Vault operations. Default is 10 seconds.
    /// </summary>
    /// <remarks>
    /// This timeout prevents operations from hanging indefinitely when Key Vault
    /// is unavailable or experiencing high latency. Adjust based on your network
    /// conditions and Key Vault response times.
    /// </remarks>
    public TimeSpan KeyVaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the maximum number of concurrent Key Vault requests. Default is 10.
    /// </summary>
    /// <remarks>
    /// This limit prevents overwhelming Key Vault with too many concurrent requests,
    /// which could lead to throttling. Adjust based on your Key Vault throughput
    /// requirements and Azure subscription limits.
    /// </remarks>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to enable fallback to environment variables. Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the system will attempt to retrieve configuration values from
    /// environment variables if Key Vault is unavailable or doesn't contain the
    /// requested secret. This provides high availability and development flexibility.
    /// </remarks>
    public bool EnableEnvironmentVariableFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable audit logging for Key Vault operations. Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, all Key Vault access operations will be logged for security
    /// monitoring and compliance purposes. This is highly recommended for production
    /// environments to track configuration access patterns.
    /// </remarks>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the retry policy for Key Vault operations.
    /// </summary>
    /// <remarks>
    /// Configure retry behavior for handling transient failures when accessing
    /// Key Vault. The default policy includes exponential backoff with jitter.
    /// </remarks>
    public KeyVaultRetryPolicy RetryPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets custom Key Vault secret name mappings.
    /// </summary>
    /// <remarks>
    /// Use this to map application configuration keys to specific Key Vault secret names.
    /// This is useful when your Key Vault naming conventions don't match your
    /// application configuration keys.
    /// </remarks>
    public Dictionary<string, string> SecretNameMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant ID for Azure authentication when using service principal authentication.
    /// </summary>
    /// <remarks>
    /// This is optional and only needed when using service principal authentication
    /// instead of managed identity. In most Azure-hosted scenarios, managed identity
    /// is preferred for better security and easier management.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client ID for Azure authentication when using managed identity or service principal.
    /// </summary>
    /// <remarks>
    /// For user-assigned managed identity, this should be the client ID of the managed identity.
    /// For service principal authentication, this should be the application ID.
    /// Leave null to use system-assigned managed identity.
    /// </remarks>
    public string? ClientId { get; set; }
}

/// <summary>
/// Configuration for Key Vault retry policy.
/// </summary>
/// <remarks>
/// This class provides fine-grained control over retry behavior for Key Vault
/// operations, helping to handle transient failures gracefully while avoiding
/// overwhelming the service with too many retries.
/// </remarks>
public sealed class KeyVaultRetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts. Default is 1 second.
    /// </summary>
    /// <remarks>
    /// The actual delay will be calculated using exponential backoff:
    /// delay = BaseDelay * (2 ^ attempt) + random jitter
    /// </remarks>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts. Default is 30 seconds.
    /// </summary>
    /// <remarks>
    /// This prevents the exponential backoff from creating extremely long delays
    /// that could impact application responsiveness.
    /// </remarks>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to add random jitter to retry delays. Default is true.
    /// </summary>
    /// <remarks>
    /// Jitter helps prevent thundering herd problems when multiple instances
    /// of the application retry at the same time. This is recommended for
    /// production deployments with multiple application instances.
    /// </remarks>
    public bool EnableJitter { get; set; } = true;
}
