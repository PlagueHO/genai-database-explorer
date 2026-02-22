# Quick start

The general steps to using the GenAI Database Explorer are as follows:

1. [Install the GenAI CLI](#install-the-genai-cli)
1. [Create a new project](#create-a-new-project)
1. [Configure the project](#configure-the-project)
1. [Extract the database schema](#extract-the-database-schema)
1. (Optional) [Add a database dictionary for each table](#add-a-database-dictionary-for-each-table)
1. [Generate the semantic model](#generate-the-semantic-model)
1. [Generate vectors](#generate-vectors)
1. (Optional) [Reconcile the vector index](#reconcile-the-vector-index)
1. [Query the semantic model](#query-the-semantic-model)

## Install the GenAI CLI

The GenAI CLI is a command-line tool that provides various commands for working with the GenAI Database Explorer. To install the GenAI CLI, follow the instructions in the [installation guide](../INSTALLATION.md).

## Deploy Azure Infrastructure (Optional)

The GenAI Database Explorer can optionally deploy supporting Azure infrastructure to provide cloud-based services for enhanced capabilities:

| Azure Service | Purpose | Required? | Notes |
|---------------|---------|-----------|-------|
| **Azure SQL Database** | Sample database (AdventureWorksLT) for experimentation | Optional | Provides a database to extract a semantic model from without needing your own database |
| **Azure OpenAI Service** (Microsoft Foundry) | Semantic enrichment and natural language querying | Required | Required to enrich the semantic model and perform natural language queries |
| **Vector storage services** (Azure AI Search or Cosmos DB) | Semantic search capabilities | Optional | If not deployed, the tool can use in-memory or local disk storage for vectors, but this is not suitable for larger databases |
| **Azure Storage Account** | Cloud storage for semantic models | Optional | Alternative to storing semantic models locally |

While the GenAI CLI can work entirely with local storage and external AI services, deploying the Azure infrastructure provides:

- A complete end-to-end environment for testing
- Scalable cloud storage for semantic models
- Integrated vector search capabilities
- Sample database with realistic data structure

### Deploy with Azure Developer CLI

This solution accelerator supports deployment using the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview).

#### Prerequisites

Before you begin, ensure you have the following prerequisites in place:

1. An active Azure subscription - [Create a free account](https://azure.microsoft.com/free/) if you don't have one.
1. [Azure Developer CLI (azd)](https://aka.ms/install-azd) Install or update to the latest version.
1. **Windows Only:** [PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) of the latest version. Ensure that PowerShell executable `pwsh.exe` is added to the `PATH` variable.

#### If you have not cloned this repository

1. Download the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview)
1. Clone and initialize this repository:

   ```bash
   azd init -t PlagueHO/genai-database-explorer
   ```

1. Authenticate the Azure Developer CLI:

   ```bash
   azd auth login
   ```

1. (Optional) Configure deployment options using environment variables (see [Configuration Options](#configuration-options) below):

   ```bash
   azd env set ENABLE_PUBLIC_NETWORK_ACCESS true
   azd env set COSMOS_DB_DEPLOY true
   azd env set AZURE_AI_SEARCH_DEPLOY true
   ```

1. Deploy the infrastructure:

   ```bash
   azd up
   ```

#### If you have already cloned this repository

1. Download the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview)
1. Navigate to the repository root directory:

   ```bash
   cd genai-database-explorer
   ```

1. Authenticate the Azure Developer CLI:

   ```bash
   azd auth login
   ```

1. (Optional) Configure deployment options using environment variables (see [Configuration Options](#configuration-options) below):

   ```bash
   azd env set ENABLE_PUBLIC_NETWORK_ACCESS true
   azd env set STORAGE_ACCOUNT_DEPLOY true
   ```

1. Deploy the infrastructure:

   ```bash
   azd up
   ```

### Configuration Options

You can customize the deployment by setting environment variables before running `azd up`. Use the `azd env set` command to configure options:

```bash
azd env set VARIABLE_NAME value
```

#### Core Infrastructure Options

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `AZURE_ENV_NAME` | Name of the environment (used for resource naming) | `azdtemp` | `genaidbexp-dev` |
| `AZURE_LOCATION` | Azure region for deployment | `EastUS2` | `West US 2` |
| `AZURE_PRINCIPAL_ID` | Object ID of user/service principal for access | Current CLI user | `00000000-0000-0000-0000-000000000000` |

#### Network Access Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `ENABLE_PUBLIC_NETWORK_ACCESS` | Enable public internet access to all resources | `true` | `false` |

> **Important:** When `ENABLE_PUBLIC_NETWORK_ACCESS` is set to `false`, resources will only be accessible through private networking. This provides enhanced security but requires VPN or Azure Bastion for access.

#### Optional Service Deployment

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `AZURE_AI_SEARCH_DEPLOY` | Deploy Azure AI Search for vector indexing | `false` | `true` |
| `COSMOS_DB_DEPLOY` | Deploy Cosmos DB for semantic model storage | `false` | `true` |
| `STORAGE_ACCOUNT_DEPLOY` | Deploy Azure Storage Account for blob storage | `false` | `true` |

#### Database Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `SQL_SERVER_USERNAME` | SQL Server administrator username | `sqladmin` | `dbadmin` |
| `SQL_SERVER_PASSWORD` | SQL Server administrator password | *Required* | `ComplexP@ssw0rd!` |
| `CLIENT_IP_ADDRESS` | Client IP address to allow access to Azure resources | *None* | `123.45.67.89` |

> **Note:** The `CLIENT_IP_ADDRESS` parameter creates access rules that allow the specified IP address to connect to both the Azure SQL Server and Azure Storage Account. This is useful for development scenarios where you need to connect from your local machine or a specific server. If not provided, no client-specific access rules will be created.

#### Example Configuration Commands

```bash
# Deploy with private networking (recommended for production)
azd env set ENABLE_PUBLIC_NETWORK_ACCESS false
azd env set AZURE_AI_SEARCH_DEPLOY true
azd env set COSMOS_DB_DEPLOY true

# Deploy with public access and client IP allowlist (recommended for development/testing)
azd env set ENABLE_PUBLIC_NETWORK_ACCESS true
azd env set STORAGE_ACCOUNT_DEPLOY true
azd env set SQL_SERVER_PASSWORD "YourSecurePassword123!"
azd env set CLIENT_IP_ADDRESS "123.45.67.89"

# Deploy minimal infrastructure (AI services only)
azd env set AZURE_AI_SEARCH_DEPLOY false
azd env set COSMOS_DB_DEPLOY false
azd env set STORAGE_ACCOUNT_DEPLOY false

# Automatically detect and allow your current public IP to access Azure resources (Linux/macOS)
azd env set CLIENT_IP_ADDRESS $(curl -s https://ipinfo.io/ip)

# Automatically detect and allow your current public IP to access Azure resources (PowerShell)
azd env set CLIENT_IP_ADDRESS (Invoke-RestMethod -Uri "https://ipinfo.io/ip" -UseBasicParsing).Trim()
```

### Accessing Deployed Resources

After deployment completes, `azd up` will output connection information for the deployed resources:

- **Microsoft Foundry Endpoint**: Use this for AI service configuration
- **SQL Server Connection**: Use this for database connectivity
- **Storage Account/Cosmos DB**: Use these for semantic model persistence
- **AI Search Service**: Use this for vector indexing capabilities

Update your project's `settings.json` file with the deployment outputs to connect your local GenAI CLI to the deployed Azure resources.

### Deleting the Deployment

To remove all deployed Azure resources:

```bash
azd down
```

To force deletion and purge soft-deleted resources:

```bash
azd down --force --purge
```

> **Warning:** This will permanently delete all resources and data. Ensure you have backed up any important data before running this command.

## Create a new project

To create a new GenAI Database Explorer project, use the `init-project` command. This command initializes a new project directory with the necessary structure and configuration files.

```bash
gaidbexp init-project --project /path/to/project
```

Replace `/path/to/project` with the desired path for the project directory. This directory must be empty.

## Configure the project

After creating a new project, you can configure it by editing the project configuration file (`settings.json`). This file contains various settings that control the behavior of the GenAI Database Explorer, including the database connection string and the connection settings to Microsoft Foundry Models.

Edit the `settings.json` file in the project directory to set the desired configuration values:

```json
{
    "SettingsVersion": "1.0.0",
    "Database": {
        "Name": "<The name of your project>",
        "Description": "<An optional description of the purpose of the database that helps ground the semantic descriptions>", // This helps ground the AI on the context of the database.
        "ConnectionString": "Server=MyServer;Database=MyDatabase;User Id=<SQL username>;Password=<SQL password>;TrustServerCertificate=True;MultipleActiveResultSets=True;",
        "AuthenticationType": "SqlAuthentication", // Authentication type: "SqlAuthentication" (default) or "EntraIdAuthentication" (for managed identity/Entra ID)
        "Schema": "dbo",
        // ... other parameters
  },
  // ... other settings
    "FoundryModels": {
        "Default": {
            "AuthenticationType": "EntraIdAuthentication", // EntraIdAuthentication (recommended), ApiKey
            // "ApiKey": "<Set your API key>", // Only required when AuthenticationType is ApiKey
            "Endpoint": "https://<Set your Microsoft Foundry endpoint>.services.ai.azure.com/" // Foundry endpoint (also accepts .openai.azure.com and .cognitiveservices.azure.com)
        },
        "ChatCompletion": {
            "DeploymentName": "<Set your chat completion deployment name>" // Recommend gpt-4.1 or gpt-4.1-mini
        },
        // Required for structured chat completion to reliably extract entity lists. Must be a model that supports structured output.
        "ChatCompletionStructured": {
            "DeploymentName": "<Set your structured chat completion deployment name>" // Recommend gpt-4.1 or gpt-4.1-mini
        },
        "Embedding": {
            "DeploymentName": "<Set your embedding deployment name>" // Recommend text-embedding-3-large/small or ada-002
        }
    },
    "VectorIndex": {
        "Provider": "Auto", // Index provider selection: Auto, AzureAISearch, CosmosDB, InMemory
        "CollectionName": "genaide-entities", // Logical index/collection name for vectors
        "PushOnGenerate": true, // Upsert vectors into the index immediately after generation
        "ProvisionIfMissing": false, // Attempt to create the index/collection if it does not exist
        "EmbeddingServiceId": "Embeddings", // The registered SK embeddings service name to use
        "ExpectedDimensions": 3072, // Expected embedding vector size (validates against model/index)
        "AllowedForRepository": ["LocalDisk", "AzureBlob", "CosmosDb"], // Allowed persistence strategies for indexing integration
        "AzureAISearch": {
                "Endpoint": "https://<Set your Azure AI Search endpoint>.search.windows.net", // Azure AI Search endpoint URL
                "IndexName": "<Set your Azure AI Search index name>", // AI Search index name to store vectors
                "ApiKey": "<Set your Azure AI Search API key>" // Admin/Query API key (use managed identity in production when possible)
        },
        "CosmosDB": {
                // When using the CosmosDB repository strategy, vectors are stored on the SAME entity documents
                // in the Entities container. Configure only vector specifics here.
                "VectorPath": "/embedding/vector",          // JSON path on the entity document holding the vector
                "DistanceFunction": "cosine",               // cosine | dotproduct | euclidean
                "IndexType": "diskANN"                      // diskANN | quantizedFlat | flat
        },
        "Hybrid": {
            "Enabled": false
        }
    }
}
```

## Database authentication options

The GenAI Database Explorer supports two authentication methods for connecting to SQL Server and Azure SQL Database:

### SQL Authentication (Default)

Uses traditional username and password authentication. This is the default setting and works with both on-premises SQL Server and Azure SQL Database.

```json
{
    "Database": {
        "ConnectionString": "Server=MyServer;Database=MyDatabase;User Id=myuser;Password=mypassword;TrustServerCertificate=True;MultipleActiveResultSets=True;",
        "AuthenticationType": "SqlAuthentication"
    }
}
```

### Microsoft Entra ID Authentication (Managed Identity)

Uses Microsoft Entra ID (formerly Azure AD) authentication with managed identity. This is recommended for applications running in Azure and provides better security by eliminating the need to store passwords. Uses the "Active Directory Default" authentication mode internally, which supports DefaultAzureCredential and multiple authentication methods.

```json
{
    "Database": {
        "ConnectionString": "Server=myserver.database.windows.net;Database=MyDatabase;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=True;",
        "AuthenticationType": "EntraIdAuthentication"
    }
}
```

**Important notes for Entra ID authentication:**

- The connection string should **not** include username/password when using Entra ID authentication
- The application will automatically add `Authentication=Active Directory Default` to the connection string
- The application must be running in an environment with Azure credentials available (Azure VM with managed identity, Azure App Service, local development with Azure CLI, etc.)
- The Entra ID identity must have appropriate permissions to access the target database
- Uses SqlClient's built-in "Active Directory Default" authentication which internally uses DefaultAzureCredential and automatically tries multiple authentication methods in order: Environment variables, Managed Identity, Visual Studio/Azure CLI, Interactive browser

## Extract the database schema

To extract the database schema, use the `extract-model` command. This command connects to the specified database and generates a JSON file containing the schema information. It will also create subdirectories for the table, view and stored procedure entities.

```bash
gaidbexp extract-model --project /path/to/project
```

## Add a database dictionary for each table

This is an optional step you can use if you have a database dictionary that provides additional information about the database schema. The dictionary should be a folder of markdown files, one for each table in the database. This process will use the `ChatCompletionStructured` model to extract the dictionary information from the markdown files.

```bash
gaidbexp data-dictionary table --project /path/to/project -d /path/to/data_dictionary/*.md
```

## Generate the semantic model

To generate the semantic model, use the `enrich-model` command. This command processes each database entity to create a semantic model that can be used for querying by passing the entity information through the `ChatCompletion` model.

```bash
gaidbexp enrich-model --project /path/to/project
```

## Generate vectors

Generate embedding vectors for semantic model entities and upsert them into the configured vector index.

```bash
# Generate embeddings for all entities using current settings
gaidbexp generate-vectors --project /path/to/project

# Force regeneration and index upsert
gaidbexp generate-vectors --project /path/to/project --overwrite

# Target a single table
gaidbexp generate-vectors table --project /path/to/project --schema-name dbo --name tblItemSellingLimit
```

## Reconcile the vector index

Reconcile the external vector index with locally persisted embeddings by re-upserting records.

```bash
# Preview reconciliation actions
gaidbexp reconcile-index --project /path/to/project --dry-run

# Perform reconciliation (writes to index)
gaidbexp reconcile-index --project /path/to/project
```

## Query the semantic model

After generating the semantic model, you can query it using the `query-model` command. This command allows you to interact with the semantic model by asking questions about the database schema and receiving responses based on the enriched semantic model.

```bash
Not implemented yet.
```
