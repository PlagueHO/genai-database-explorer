using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class FoundryModelsDefaultSettings
{
    // The settings key that contains the Default Foundry Models settings
    public const string PropertyName = "Default";

    /// <summary>
    /// Specifies the authentication method to use when connecting to Foundry Models endpoints.
    /// Defaults to EntraIdAuthentication (managed identity/DefaultAzureCredential).
    /// </summary>
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.EntraIdAuthentication;

    public string? ApiKey { get; set; }

    [Required]
    [Url(ErrorMessage = "Endpoint must be a valid URL")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Optional Azure tenant ID to use with DefaultAzureCredential when using EntraIdAuthentication.
    /// If not specified, DefaultAzureCredential will use the default tenant.
    /// Useful when the user is signed into multiple tenants or when the resource is in a different tenant.
    /// </summary>
    public string? TenantId { get; set; }
}
