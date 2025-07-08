using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GenAIDBExplorer.Core.Security;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Security;

/// <summary>
/// Secure JSON serializer implementation that provides protection against injection attacks
/// and secure handling of sensitive data during JSON operations.
/// </summary>
/// <remarks>
/// This implementation provides comprehensive security features:
/// - Input validation to prevent malicious JSON payloads
/// - Output sanitization to prevent data leakage
/// - Protection against JSON injection and deserialization attacks
/// - Depth and size limits to prevent denial-of-service attacks
/// - Audit logging for security monitoring and compliance
/// - Safe error handling that doesn't expose sensitive information
/// 
/// Security validations performed:
/// - JSON structure validation (balanced brackets, proper nesting)
/// - Content validation (dangerous patterns, injection attempts)
/// - Size and depth limits enforcement
/// - Unicode normalization and validation
/// - Potential exploit pattern detection
/// </remarks>
public class SecureJsonSerializer(ILogger<SecureJsonSerializer> logger) : ISecureJsonSerializer
{
    private const int MaxJsonSizeBytes = 50 * 1024 * 1024; // 50MB limit
    private const int MaxJsonDepth = 64; // Maximum nesting depth
    private const int MaxStringLength = 1024 * 1024; // 1MB string limit
    private const int MaxArrayLength = 100000; // Maximum array elements
    
    private static readonly JsonSerializerOptions DefaultSecureOptions = new()
    {
        MaxDepth = MaxJsonDepth,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string[] DangerousPatterns = [
        @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", // Script tags
        @"javascript:", // JavaScript protocol
        @"data:(?!image\/)", // Data URI (except images)
        @"vbscript:", // VBScript protocol
        @"on\w+\s*=", // Event handlers (onclick, onload, etc.)
        @"<%.*?%>", // Server-side includes
        @"\$\{.*?\}", // Template injection
        @"#\{.*?\}", // Ruby/ERB injection
        @"\{\{.*?\}\}", // Handlebars/Mustache injection
        @"eval\s*\(", // JavaScript eval
        @"Function\s*\(", // JavaScript Function constructor
        @"setTimeout\s*\(", // JavaScript setTimeout
        @"setInterval\s*\(", // JavaScript setInterval
        @"document\.", // DOM access
        @"window\.", // Window object access
        @"location\.", // Location object access
        @"alert\s*\(", // Alert dialogs
        @"confirm\s*\(", // Confirm dialogs
        @"prompt\s*\(" // Prompt dialogs
    ];

    private static readonly Regex DangerousPatternsRegex = new(
        string.Join("|", DangerousPatterns),
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline
    );

    /// <inheritdoc />
    public async Task<string> SerializeAsync<T>(T value, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        
        try
        {
            logger.LogDebug("Starting secure JSON serialization for type {TypeName}", typeof(T).Name);
            
            var jsonOptions = options ?? DefaultSecureOptions;
            var json = JsonSerializer.Serialize(value, jsonOptions);
            
            // Validate the serialized JSON for security
            await ValidateSerializedJsonAsync(json);
            
            logger.LogDebug("JSON serialization completed successfully, size: {Size} bytes", 
                Encoding.UTF8.GetByteCount(json));
            
            return json;
        }
        catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
        {
            logger.LogError(ex, "Secure JSON serialization failed for type {TypeName}", typeof(T).Name);
            throw new InvalidOperationException($"Secure JSON serialization failed for type {typeof(T).Name}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<T?> DeserializeAsync<T>(string json, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        
        try
        {
            logger.LogDebug("Starting secure JSON deserialization for type {TypeName}", typeof(T).Name);
            
            // Validate JSON security before processing
            if (!await ValidateJsonSecurityAsync(json))
            {
                throw new ArgumentException("JSON content failed security validation", nameof(json));
            }
            
            var jsonOptions = options ?? DefaultSecureOptions;
            var result = JsonSerializer.Deserialize<T>(json, jsonOptions);
            
            logger.LogDebug("JSON deserialization completed successfully for type {TypeName}", typeof(T).Name);
            
            return result;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization failed - malformed JSON for type {TypeName}", typeof(T).Name);
            throw new JsonException($"JSON deserialization failed for type {typeof(T).Name}: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            logger.LogError(ex, "Secure JSON deserialization failed for type {TypeName}", typeof(T).Name);
            throw new InvalidOperationException($"Secure JSON deserialization failed for type {typeof(T).Name}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateJsonSecurityAsync(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        
        try
        {
            logger.LogTrace("Validating JSON security, size: {Size} bytes", Encoding.UTF8.GetByteCount(json));
            
            // Check size limits
            if (Encoding.UTF8.GetByteCount(json) > MaxJsonSizeBytes)
            {
                logger.LogWarning("JSON size exceeds maximum allowed size of {MaxSize} bytes", MaxJsonSizeBytes);
                return false;
            }
            
            // Validate JSON structure first
            if (!await ValidateJsonStructureAsync(json))
            {
                logger.LogWarning("JSON structure validation failed");
                return false;
            }
            
            // Check for dangerous patterns
            if (DangerousPatternsRegex.IsMatch(json))
            {
                logger.LogWarning("JSON content contains potentially dangerous patterns");
                return false;
            }
            
            // Additional security validations using existing security utilities
            try
            {
                EntityNameSanitizer.ValidateInputSecurity(json, nameof(json));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "JSON content failed input security validation");
                return false;
            }
            
            // Validate Unicode normalization
            try
            {
                var normalized = json.Normalize(NormalizationForm.FormC);
                if (!string.Equals(json, normalized, StringComparison.Ordinal))
                {
                    logger.LogWarning("JSON content contains non-normalized Unicode characters");
                    return false;
                }
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "JSON Unicode normalization failed");
                return false;
            }
            
            logger.LogTrace("JSON security validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON security validation failed with exception");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> SanitizeJsonAsync(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        
        try
        {
            logger.LogDebug("Sanitizing JSON content, size: {Size} bytes", Encoding.UTF8.GetByteCount(json));
            
            // Normalize Unicode characters
            var sanitized = json.Normalize(NormalizationForm.FormC);
            
            // Remove dangerous patterns by replacing with safe alternatives
            foreach (var pattern in DangerousPatterns)
            {
                sanitized = Regex.Replace(sanitized, pattern, "[SANITIZED]", RegexOptions.IgnoreCase);
            }
            
            // Validate the sanitized result
            if (!await ValidateJsonStructureAsync(sanitized))
            {
                throw new InvalidOperationException("JSON sanitization resulted in invalid JSON structure");
            }
            
            logger.LogDebug("JSON sanitization completed, size: {Size} bytes", 
                Encoding.UTF8.GetByteCount(sanitized));
            
            return sanitized;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON sanitization failed");
            throw new InvalidOperationException("JSON sanitization failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> SerializeWithAuditAsync<T>(T value, string operationContext, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationContext);
        
        try
        {
            logger.LogInformation("Starting audited JSON serialization for type {TypeName}, context: {Context}", 
                typeof(T).Name, operationContext);
            
            var result = await SerializeAsync(value, options);
            
            logger.LogInformation("Audited JSON serialization completed successfully for type {TypeName}, " +
                "context: {Context}, size: {Size} bytes", 
                typeof(T).Name, operationContext, Encoding.UTF8.GetByteCount(result));
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audited JSON serialization failed for type {TypeName}, context: {Context}", 
                typeof(T).Name, operationContext);
            throw;
        }
    }

    /// <summary>
    /// Validates the basic JSON structure for correctness and security.
    /// </summary>
    /// <param name="json">The JSON content to validate.</param>
    /// <returns>True if the JSON structure is valid; otherwise, false.</returns>
    private static async Task<bool> ValidateJsonStructureAsync(string json)
    {
        try
        {
            // Try to parse as JsonDocument to validate structure
            using var document = JsonDocument.Parse(json);
            
            // Check depth and complexity
            return await ValidateJsonElementAsync(document.RootElement, 0);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Recursively validates JSON elements for security constraints.
    /// </summary>
    /// <param name="element">The JSON element to validate.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>True if the element passes validation; otherwise, false.</returns>
    private static async Task<bool> ValidateJsonElementAsync(JsonElement element, int depth)
    {
        // Check maximum depth
        if (depth > MaxJsonDepth)
        {
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var propertyCount = 0;
                foreach (var property in element.EnumerateObject())
                {
                    propertyCount++;
                    if (propertyCount > MaxArrayLength) // Use same limit for properties
                    {
                        return false;
                    }
                    
                    // Validate property name
                    if (property.Name.Length > MaxStringLength)
                    {
                        return false;
                    }
                    
                    // Recursively validate property value
                    if (!await ValidateJsonElementAsync(property.Value, depth + 1))
                    {
                        return false;
                    }
                }
                break;

            case JsonValueKind.Array:
                var arrayCount = 0;
                foreach (var item in element.EnumerateArray())
                {
                    arrayCount++;
                    if (arrayCount > MaxArrayLength)
                    {
                        return false;
                    }
                    
                    // Recursively validate array item
                    if (!await ValidateJsonElementAsync(item, depth + 1))
                    {
                        return false;
                    }
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                if (stringValue != null && stringValue.Length > MaxStringLength)
                {
                    return false;
                }
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                // These are safe primitive types
                break;

            default:
                // Unknown type - reject for safety
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates serialized JSON output for additional security checks.
    /// </summary>
    /// <param name="json">The serialized JSON to validate.</param>
    private async Task ValidateSerializedJsonAsync(string json)
    {
        // Check size after serialization
        if (Encoding.UTF8.GetByteCount(json) > MaxJsonSizeBytes)
        {
            throw new InvalidOperationException($"Serialized JSON exceeds maximum size of {MaxJsonSizeBytes} bytes");
        }
        
        // Validate structure
        if (!await ValidateJsonStructureAsync(json))
        {
            throw new InvalidOperationException("Serialized JSON failed structure validation");
        }
        
        // Check for potential information leakage patterns
        var sensitivePatterns = new[]
        {
            @"password", @"secret", @"key", @"token", @"credential",
            @"connectionstring", @"apikey", @"accesskey"
        };
        
        foreach (var pattern in sensitivePatterns)
        {
            if (Regex.IsMatch(json, pattern, RegexOptions.IgnoreCase))
            {
                logger.LogWarning("Serialized JSON may contain sensitive information pattern: {Pattern}", pattern);
                // Note: We log but don't fail - this is for monitoring purposes
            }
        }
    }
}
