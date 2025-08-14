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

## Create a new project

To create a new GenAI Database Explorer project, use the `init-project` command. This command initializes a new project directory with the necessary structure and configuration files.

```bash
gaidbexp init-project --project /path/to/project
```

Replace `/path/to/project` with the desired path for the project directory. This directory must be empty.

## Configure the project

After creating a new project, you can configure it by editing the project configuration file (`settings.json`). This file contains various settings that control the behavior of the GenAI Database Explorer, including the database connection string and the connection settings to the Azure OpenAI or OpenAI services.

Edit the `settings.json` file in the project directory to set the desired configuration values:

```json
{
    "SettingsVersion": "1.0.0",
    "Database": {
        "Name": "<The name of your project>",
        "Description": "<An optional description of the purpose of the database that helps ground the semantic descriptions>", // This helps ground the AI on the context of the database.
        "ConnectionString": "Server=MyServer;Database=MyDatabase;User Id=<SQL username>;Password=<SQL password>;TrustServerCertificate=True;MultipleActiveResultSets=True;",
        "Schema": "dbo",
        // ... other parameters
  },
  // ... other settings
    "OpenAIService": {
        "Default": {
            "ServiceType": "AzureOpenAI", // AzureOpenAI, OpenAI
            // "OpenAIKey": "<Set your OpenAI API key>"
            "AzureOpenAIKey": "<Set your Azure OpenAI API key>", // Azure OpenAI key. If not provided, will attempt using Azure Default Credential
            "AzureOpenAIEndpoint": "https://<Set your Azure OpenAI endpoint>.cognitiveservices.azure.com/" // Azure OpenAI endpoint
            // "AzureOpenAIAppId": "" // Azure OpenAI App Id
        },
        "ChatCompletion": {
            // "ModelId": "gpt-4.1-mini-2025-04-14", // Only required when using OpenAI. Recommend gpt-4.1-2025-04-14 or gpt-4.1-mini-2025-04-14 (or above)
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Recommend gpt-4o or gpt-4o-mini
        },
        // Required for structured chat completion to reliably extract entity lists. Must be a model that supports structured output.
        "ChatCompletionStructured": {
            // "ModelId": "gpt-4.1-mini-2025-04-14", // Only required when using OpenAI. Recommend gpt-4.1-2025-04-14 or gpt-4.1-mini-2025-04-14 (or above)
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Recommend gpt-4o (2024-08-06 or later)
        },
        "Embedding": {
            // "ModelId": "gpt-4o-mini-2024-07-18", // Only required when using OpenAI. Recommend gpt-4o-2024-08-06 or gpt-4o-mini-2024-07-18
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Used for vector embeddings generation (recommend text-embedding-3-large/small or ada-002)
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
gaidbexp generate-vectors table --project /path/to/project --schema dbo --name tblItemSellingLimit
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
