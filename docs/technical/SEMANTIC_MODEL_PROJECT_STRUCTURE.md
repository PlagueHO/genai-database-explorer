# Semantic Model Project Structure

A Generative AI Database Explorer project stores all the necessary files and configurations to work with a database schema and its semantic model. At the minimum, a project contains the following structure:

```text
project/
├── settings.json
```

However, depending on the storage strategy and features used, the project structure can expand to include additional directories and files. Below is a detailed description of the project structure and its components.

```text
project/
├── settings.json
└── my_app_model/
    ├── semanticmodel.json
    ├── tables/
    │   ├── table1.json
    │   ├── table2.json
    │   └── ...
    ├── views/
    │   ├── view1.json
    │   ├── view2.json
    │   └── ...
    └── stored_procedures/
        ├── procedure1.json
        ├── procedure2.json
        └── ...
```

## Settings File

The `settings.json` file contains configuration settings for the project, such as the database connection details, persistence strategy, and other project-specific options. This file is essential for initializing and managing the project.

### Configuration Structure

The settings file supports three persistence strategies for semantic model storage:

- **LocalDisk**: Stores semantic models in a local directory (development scenarios) - Default strategy.
- **AzureBlob**: Stores semantic models in Azure Blob Storage (cloud scenarios)  
- **Cosmos**: Stores semantic models in Azure Cosmos DB (global scale scenarios)

Each persistence strategy has its own dedicated configuration section under `SemanticModelRepository`:

```json
{
    "SemanticModel": {
        "PersistenceStrategy": "LocalDisk", // Default strategy is LocalDisk
        "MaxDegreeOfParallelism": 10
    },
    "SemanticModelRepository": {
        "LocalDisk": {
            "Directory": "SemanticModel"
        },
        "AzureBlobStorage": {
            "AccountEndpoint": "https://mystorageaccount.blob.core.windows.net",
            "ContainerName": "semantic-models"
        },
        "CosmosDb": {
            "AccountEndpoint": "https://mycosmosaccount.documents.azure.com:443/",
            "DatabaseName": "SemanticModels",
            "ModelsContainerName": "Models"
        }
    }
}
```
