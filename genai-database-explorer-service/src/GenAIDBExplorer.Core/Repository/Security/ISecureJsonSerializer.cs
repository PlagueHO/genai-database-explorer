using System.Text.Json;

namespace GenAIDBExplorer.Core.Repository.Security;

/// <summary>
/// Interface for secure JSON serialization that prevents injection attacks and provides
/// safe handling of sensitive data during serialization and deserialization operations.
/// </summary>
/// <remarks>
/// This interface provides enterprise-grade security features for JSON operations:
/// - Input validation to prevent malicious JSON payloads
/// - Output sanitization to prevent data leakage
/// - Protection against deserialization attacks
/// - Secure handling of sensitive data during serialization
/// - Support for secure configuration management
/// 
/// Security features implemented:
/// - JSON injection attack prevention
/// - Deserialization bomb protection (depth and size limits)
/// - Malicious payload detection and validation
/// - Safe error handling without information disclosure
/// - Audit logging for security-related operations
/// </remarks>
public interface ISecureJsonSerializer
{
    /// <summary>
    /// Serializes an object to a JSON string with security validation and sanitization.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="options">Optional JSON serializer options for customization.</param>
    /// <returns>A secure JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails security validation.</exception>
    Task<string> SerializeAsync<T>(T value, JsonSerializerOptions? options = null);

    /// <summary>
    /// Deserializes a JSON string to an object with security validation and input sanitization.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options for customization.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentException">Thrown when JSON contains malicious content.</exception>
    /// <exception cref="JsonException">Thrown when JSON is malformed or fails security validation.</exception>
    Task<T?> DeserializeAsync<T>(string json, JsonSerializerOptions? options = null);

    /// <summary>
    /// Validates JSON content for potential security threats before processing.
    /// </summary>
    /// <param name="json">The JSON content to validate.</param>
    /// <returns>True if the JSON is safe to process; otherwise, false.</returns>
    Task<bool> ValidateJsonSecurityAsync(string json);

    /// <summary>
    /// Sanitizes JSON content by removing or escaping potentially dangerous elements.
    /// </summary>
    /// <param name="json">The JSON content to sanitize.</param>
    /// <returns>Sanitized JSON content safe for processing.</returns>
    Task<string> SanitizeJsonAsync(string json);

    /// <summary>
    /// Serializes an object with additional security context for audit logging.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="operationContext">Context information for security auditing.</param>
    /// <param name="options">Optional JSON serializer options for customization.</param>
    /// <returns>A secure JSON string representation of the object.</returns>
    Task<string> SerializeWithAuditAsync<T>(T value, string operationContext, JsonSerializerOptions? options = null);
}
