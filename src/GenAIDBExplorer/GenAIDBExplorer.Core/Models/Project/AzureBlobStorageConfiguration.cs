using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Custom validation attribute that validates URLs only when they are not null or empty.
/// </summary>
public sealed class ConditionalUrlAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates that the value is a valid URL if it is not null or empty.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is null, empty, or a valid URL; otherwise, false.</returns>
    public override bool IsValid(object? value)
    {
        // Allow null or empty values - this handles the case where the URL is optional
        if (value is null or "")
        {
            return true;
        }

        // If we have a value, validate it as a URL
        var urlAttribute = new UrlAttribute();
        return urlAttribute.IsValid(value);
    }

    /// <summary>
    /// Gets the default error message for invalid URLs.
    /// </summary>
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid fully-qualified http, https, or ftp URL.";
    }
}

/// <summary>
/// Configuration options for Azure Blob Storage persistence strategy.
/// </summary>
public sealed class AzureBlobStorageConfiguration
{
    /// <summary>
    /// Configuration section name for Azure Blob Storage settings.
    /// </summary>
    public const string SectionName = "SemanticModelRepository:AzureBlobStorage";

    /// <summary>
    /// Gets or sets the Azure Storage account endpoint URI.
    /// Example: https://mystorageaccount.blob.core.windows.net
    /// </summary>
    [Required]
    [Url]
    public required string AccountEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the container to store semantic models.
    /// Default is "semantic-models".
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$",
        ErrorMessage = "Container name must be 3-63 characters, lowercase letters, numbers, and hyphens only")]
    public string ContainerName { get; set; } = "semantic-models";

    /// <summary>
    /// Gets or sets the optional prefix for all blob names.
    /// Useful for organizing models in a shared container.
    /// </summary>
    public string? BlobPrefix { get; set; }

    /// <summary>
    /// Gets or sets the timeout for blob operations in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    [Range(30, 3600)]
    public int OperationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum concurrent operations for batch uploads.
    /// Default is 4.
    /// </summary>
    [Range(1, 16)]
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to enable server-side encryption with customer-managed keys.
    /// Default is false (uses Microsoft-managed keys).
    /// </summary>
    public bool UseCustomerManagedKeys { get; set; } = false;

    /// <summary>
    /// Gets or sets the Key Vault key URL for customer-managed encryption.
    /// Required if UseCustomerManagedKeys is true.
    /// Only validated as a URL if not null or empty.
    /// </summary>
    [ConditionalUrl]
    public string? CustomerManagedKeyUrl { get; set; }
}
