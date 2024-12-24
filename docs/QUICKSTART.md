# Quick start

The general steps to using the GenAI Database Explorer are as follows:

1. [Install the GenAI CLI](#install-the-genai-cli)
1. [Create a new project](#create-a-new-project)
1. [Configure the project](#configure-the-project)
1. [Extract the database schema](#extract-the-database-schema)
1. (Optional) [Add a database dictionary](#add-a-database-dictionary)
1. [Generate the semantic model](#generate-the-semantic-model)
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
            // "ModelId": "gpt-4o-mini-2024-07-18", // Only required when using OpenAI. Recommend gpt-4o-2024-08-06 or gpt-4o-mini-2024-07-18
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Recommend gpt-4o or gpt-4o-mini
        },
        // Required for structured chat completion to reliably extract entity lists. Must be a model that supports structured output.
        "ChatCompletionStructured": {
            // "ModelId": "gpt-4o-mini-2024-07-18", // Only required when using OpenAI. Recommend gpt-4o-2024-08-06 or gpt-4o-mini-2024-07-18
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Recommend gpt-4o (2024-08-06 or later)
        },
        "Embedding": {
            // "ModelId": "gpt-4o-mini-2024-07-18", // Only required when using OpenAI. Recommend gpt-4o-2024-08-06 or gpt-4o-mini-2024-07-18
            "AzureOpenAIDeploymentId": "<Set your Azure OpenAI deployment id>" // Only required when using Azure OpenAI. Recommend text-embedding-3-large, text-embedding-3-small or text-embedding-ada-002
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

## Query the semantic model

After generating the semantic model, you can query it using the `query-model` command. This command allows you to interact with the semantic model by asking questions about the database schema and receiving responses based on the enriched semantic model.
```bash
Not implemented yet.
```