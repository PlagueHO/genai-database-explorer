namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Specifies the authentication method to use when connecting to Azure OpenAI services.
/// </summary>
public enum AzureOpenAIAuthenticationType
{
    /// <summary>
    /// Use Azure OpenAI API key authentication.
    /// This is the traditional authentication method using API keys from the Azure OpenAI resource.
    /// </summary>
    ApiKey,

    /// <summary>
    /// Use Microsoft Entra ID (Azure Active Directory) authentication.
    /// This uses DefaultAzureCredential which supports managed identities, Visual Studio, Azure CLI, and other credential sources.
    /// </summary>
    EntraIdAuthentication
}