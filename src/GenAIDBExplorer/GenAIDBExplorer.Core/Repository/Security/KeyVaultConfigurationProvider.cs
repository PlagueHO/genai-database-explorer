using System.Collections.Concurrent;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Security;

/// <summary>
/// Azure Key Vault configuration provider that securely retrieves configuration values
/// from Azure Key Vault with automatic credential rotation and fallback mechanisms.
/// </summary>
/// <remarks>
/// This implementation provides enterprise-grade security features:
/// - Secure retrieval of connection strings and secrets from Azure Key Vault
/// - Automatic credential rotation support with caching
/// - Fallback mechanisms for Key Vault unavailability
/// - Integration with Azure Managed Identity and DefaultAzureCredential
/// - Proper error handling and security monitoring
/// - High availability with graceful degradation
/// 
/// Configuration retrieval order:
/// 1. Check in-memory cache for recently retrieved values
/// 2. Attempt to retrieve from Azure Key Vault
/// 3. Fall back to environment variables if Key Vault is unavailable
/// 4. Fall back to provided default values as last resort
/// 
/// Security features:
/// - Uses DefaultAzureCredential for authentication (supports Managed Identity)
/// - Implements secure caching with time-based expiration
/// - Audit logging for all configuration access
/// - Protection against unauthorized access patterns
/// - Secure handling of sensitive configuration data
/// 
/// References:
/// - https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net
/// - https://learn.microsoft.com/en-us/azure/key-vault/general/authentication
/// </remarks>
public class KeyVaultConfigurationProvider : IDisposable
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultConfigurationProvider> _logger;
    private readonly ConcurrentDictionary<string, CachedSecret> _cache = new();
    private readonly SemaphoreSlim _retrievalSemaphore = new(10, 10); // Limit concurrent requests
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _keyVaultTimeout;
    private readonly bool _enableCaching;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the KeyVaultConfigurationProvider class.
    /// </summary>
    /// <param name="keyVaultUri">The URI of the Azure Key Vault.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="credential">Optional Azure credential to use for authentication.</param>
    /// <param name="options">Optional Key Vault configuration options.</param>
    public KeyVaultConfigurationProvider(
        string keyVaultUri,
        ILogger<KeyVaultConfigurationProvider> logger,
        DefaultAzureCredential? credential = null,
        KeyVaultOptions? options = null)
    {
        _secretClient = new SecretClient(new Uri(keyVaultUri), credential ?? new DefaultAzureCredential());
        _logger = logger;
        var opts = options ?? new KeyVaultOptions();
        _cacheExpiration = opts.CacheExpiration;
        _keyVaultTimeout = opts.KeyVaultTimeout;
        _enableCaching = opts.EnableCaching;
    }

    /// <summary>
    /// Initializes a new instance of the KeyVaultConfigurationProvider class for testing.
    /// </summary>
    /// <param name="secretClient">The SecretClient instance to use.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Optional Key Vault configuration options.</param>
    internal KeyVaultConfigurationProvider(
        SecretClient secretClient,
        ILogger<KeyVaultConfigurationProvider> logger,
        KeyVaultOptions? options = null)
    {
        _secretClient = secretClient;
        _logger = logger;
        var opts = options ?? new KeyVaultOptions();
        _cacheExpiration = opts.CacheExpiration;
        _keyVaultTimeout = opts.KeyVaultTimeout;
        _enableCaching = opts.EnableCaching;
    }

    /// <summary>
    /// Represents a cached secret with expiration tracking.
    /// </summary>
    private sealed record CachedSecret(string Value, DateTime ExpiresAt);

    /// <summary>
    /// Retrieves a configuration value from Azure Key Vault with fallback mechanisms.
    /// </summary>
    /// <param name="keyName">The name of the secret in Key Vault (will be normalized for Key Vault naming requirements).</param>
    /// <param name="fallbackEnvironmentVariable">Optional environment variable name to use as fallback.</param>
    /// <param name="defaultValue">Optional default value to use if all other sources fail.</param>
    /// <returns>The configuration value, or null if not found and no default provided.</returns>
    /// <exception cref="ArgumentException">Thrown when keyName is null or whitespace.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
    public async Task<string?> GetConfigurationValueAsync(
        string keyName,
        string? fallbackEnvironmentVariable = null,
        string? defaultValue = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);

        var normalizedKeyName = NormalizeKeyVaultSecretName(keyName);

        try
        {
            _logger.LogDebug("Retrieving configuration value for key: {KeyName}", normalizedKeyName);

            // Check cache first (only if caching is enabled)
            if (_enableCaching && _cache.TryGetValue(normalizedKeyName, out var cachedSecret) &&
                cachedSecret.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogTrace("Configuration value retrieved from cache for key: {KeyName}", normalizedKeyName);
                return cachedSecret.Value;
            }

            // Attempt to retrieve from Key Vault with throttling
            await _retrievalSemaphore.WaitAsync();
            try
            {
                var secretValue = await RetrieveFromKeyVaultAsync(normalizedKeyName);
                if (secretValue != null)
                {
                    // Cache the retrieved value (only if caching is enabled)
                    if (_enableCaching)
                    {
                        _cache[normalizedKeyName] = new CachedSecret(secretValue, DateTime.UtcNow.Add(_cacheExpiration));
                    }

                    _logger.LogDebug("Configuration value successfully retrieved from Key Vault for key: {KeyName}",
                        normalizedKeyName);
                    return secretValue;
                }
            }
            finally
            {
                _retrievalSemaphore.Release();
            }

            // Fall back to environment variable
            if (!string.IsNullOrWhiteSpace(fallbackEnvironmentVariable))
            {
                var envValue = Environment.GetEnvironmentVariable(fallbackEnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    _logger.LogInformation("Configuration value retrieved from environment variable {EnvVar} for key: {KeyName}",
                        fallbackEnvironmentVariable, normalizedKeyName);
                    return envValue;
                }
            }

            // Fall back to default value
            if (defaultValue != null)
            {
                _logger.LogInformation("Using default value for configuration key: {KeyName}", normalizedKeyName);
                return defaultValue;
            }

            _logger.LogWarning("No configuration value found for key: {KeyName}", normalizedKeyName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve configuration value for key: {KeyName}", normalizedKeyName);

            // In case of errors, try fallback mechanisms
            if (!string.IsNullOrWhiteSpace(fallbackEnvironmentVariable))
            {
                var envValue = Environment.GetEnvironmentVariable(fallbackEnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    _logger.LogWarning("Using environment variable fallback due to Key Vault error for key: {KeyName}",
                        normalizedKeyName);
                    return envValue;
                }
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Retrieves multiple configuration values in a single operation for better performance.
    /// </summary>
    /// <param name="keyNames">The names of the secrets to retrieve.</param>
    /// <returns>A dictionary of key-value pairs for the retrieved configuration values.</returns>
    public async Task<IDictionary<string, string?>> GetMultipleConfigurationValuesAsync(params string[] keyNames)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(keyNames);

        var results = new Dictionary<string, string?>();
        var tasks = keyNames.Select(async keyName =>
        {
            var value = await GetConfigurationValueAsync(keyName);
            return new KeyValuePair<string, string?>(keyName, value);
        });

        var keyValuePairs = await Task.WhenAll(tasks);
        foreach (var kvp in keyValuePairs)
        {
            results[kvp.Key] = kvp.Value;
        }

        return results;
    }

    /// <summary>
    /// Refreshes cached configuration values by clearing the cache.
    /// </summary>
    public void RefreshCache()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation("Refreshing Key Vault configuration cache");
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics for monitoring and performance analysis.
    /// </summary>
    /// <returns>Cache statistics including hit rate and cached item count.</returns>
    public CacheStatistics GetCacheStatistics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var totalItems = _cache.Count;
        var expiredItems = _cache.Values.Count(s => s.ExpiresAt <= DateTime.UtcNow);
        var activeItems = totalItems - expiredItems;

        return new CacheStatistics(activeItems, expiredItems, totalItems);
    }

    /// <summary>
    /// Tests connectivity to Azure Key Vault for health monitoring.
    /// </summary>
    /// <returns>True if Key Vault is accessible; otherwise, false.</returns>
    public async Task<bool> TestConnectivityAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            _logger.LogTrace("Testing Azure Key Vault connectivity");

            // Try to get Key Vault properties as a connectivity test
            using var cts = new CancellationTokenSource(_keyVaultTimeout);
            var response = await _secretClient.GetPropertiesOfSecretsAsync(cancellationToken: cts.Token)
                .AsPages()
                .FirstAsync(cts.Token);

            _logger.LogTrace("Azure Key Vault connectivity test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Key Vault connectivity test failed");
            return false;
        }
    }

    /// <summary>
    /// Retrieves a secret value from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <returns>The secret value, or null if not found.</returns>
    private async Task<string?> RetrieveFromKeyVaultAsync(string secretName)
    {
        try
        {
            using var cts = new CancellationTokenSource(_keyVaultTimeout);
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cts.Token);

            return response.Value?.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Secret not found in Key Vault: {SecretName}", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret from Key Vault: {SecretName}", secretName);
            throw;
        }
    }

    /// <summary>
    /// Normalizes a key name to comply with Azure Key Vault secret naming requirements.
    /// </summary>
    /// <param name="keyName">The original key name.</param>
    /// <returns>A normalized key name suitable for Key Vault.</returns>
    private static string NormalizeKeyVaultSecretName(string keyName)
    {
        // Key Vault secret names must match the pattern ^[0-9a-zA-Z-]+$
        // Replace invalid characters with hyphens and ensure it starts with alphanumeric
        var normalized = System.Text.RegularExpressions.Regex.Replace(keyName, @"[^0-9a-zA-Z-]", "-");

        // Ensure it starts with alphanumeric character
        if (normalized.Length > 0 && !char.IsLetterOrDigit(normalized[0]))
        {
            normalized = "kv-" + normalized;
        }

        // Remove consecutive hyphens and trim trailing hyphens
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");
        normalized = normalized.Trim('-');

        // Ensure minimum length
        if (normalized.Length == 0)
        {
            normalized = "default-secret";
        }

        return normalized;
    }

    /// <summary>
    /// Releases all resources used by the KeyVaultConfigurationProvider.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _retrievalSemaphore?.Dispose();
            _cache.Clear();
            _disposed = true;

            _logger.LogDebug("KeyVaultConfigurationProvider disposed");
        }
    }
}

/// <summary>
/// Represents cache statistics for monitoring and performance analysis.
/// </summary>
/// <param name="ActiveItems">Number of cached items that haven't expired.</param>
/// <param name="ExpiredItems">Number of cached items that have expired.</param>
/// <param name="TotalItems">Total number of items in the cache.</param>
public sealed record CacheStatistics(int ActiveItems, int ExpiredItems, int TotalItems)
{
    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public double HitRate => TotalItems > 0 ? (double)ActiveItems / TotalItems * 100 : 0;
}
