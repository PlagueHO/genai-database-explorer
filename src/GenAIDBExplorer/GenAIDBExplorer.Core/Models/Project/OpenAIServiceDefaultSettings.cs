using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceDefaultSettings
{
    // The settings key that contains the Default OpenAI settings
    public const string PropertyName = "Default";

    [Required, NotEmptyOrWhitespace]
    public string ServiceType { get; set; } = "AzureOpenAI";

    /// <summary>
    /// Specifies the authentication method to use when connecting to Azure OpenAI services.
    /// Defaults to EntraIdAuthentication (managed identity/DefaultAzureCredential).
    /// </summary>
    public AzureOpenAIAuthenticationType AzureAuthenticationType { get; set; } = AzureOpenAIAuthenticationType.EntraIdAuthentication;

    [RequiredOnPropertyValue(nameof(ServiceType), "OpenAI")]
    public string? OpenAIKey { get; set; }

    // Note: Complex validation for AzureOpenAIKey (requires both ServiceType="AzureOpenAI" AND AuthenticationType="ApiKey")
    // is handled in the ValidateOpenAIConfiguration method instead of using multiple validation attributes
    public string? AzureOpenAIKey { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    [Url(ErrorMessage = "AzureOpenAIEndpoint must be a valid URL")]
    public string? AzureOpenAIEndpoint { get; set; }

    /// <summary>
    /// Optional Azure tenant ID to use with DefaultAzureCredential when using EntraIdAuthentication.
    /// If not specified, DefaultAzureCredential will use the default tenant.
    /// Useful when the user is signed into multiple tenants or when the resource is in a different tenant.
    /// </summary>
    public string? TenantId { get; set; }

    public string? AzureOpenAIAppId { get; set; }
}
