namespace GenAIDBExplorer.Core.Models.Project;

/// <summary>
/// Specifies the authentication method to use when connecting to the database.
/// </summary>
public enum DatabaseAuthenticationType
{
    /// <summary>
    /// Use SQL Server authentication with username and password from the connection string.
    /// This is the traditional authentication method using SQL Server logins.
    /// </summary>
    SqlAuthentication,

    /// <summary>
    /// Use Microsoft Entra ID (Azure Active Directory) authentication.
    /// This uses the "Active Directory Default" authentication mode which supports
    /// managed identities, Visual Studio, Azure CLI, and other credential sources.
    /// </summary>
    EntraIdAuthentication
}