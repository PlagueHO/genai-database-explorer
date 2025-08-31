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
- **CosmosDb**: Stores semantic models in Azure Cosmos DB (global scale scenarios)

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
        "AzureBlob": {
            "AccountEndpoint": "https://mystorageaccount.blob.core.windows.net",
            "ContainerName": "semantic-models"
        },
        "CosmosDb": {
            "AccountEndpoint": "https://mycosmosaccount.documents.azure.com:443/",
            "DatabaseName": "SemanticModels",
            "ModelsContainerName": "Models"
        },
        "LazyLoading": {
            "Enabled": true
        },
        "Caching": {
            "Enabled": true,
            "ExpirationMinutes": 30
        },
        "ChangeTracking": {
            "Enabled": true
        },
        "PerformanceMonitoring": {
            "Enabled": true,
            "DetailedTiming": false,
            "MetricsEnabled": true
        },
        "MaxConcurrentOperations": 10
    }
}
```

### Performance and Behavior Configuration

The `SemanticModelRepository` section now includes additional configuration options to control performance and behavior:

- **LazyLoading**: Controls whether entity collections (tables, views, stored procedures) are loaded on-demand rather than eagerly
- **Caching**: Enables in-memory caching of loaded semantic models with configurable expiration
- **ChangeTracking**: Enables tracking of modifications to entities for more efficient saves  
- **PerformanceMonitoring**: Configures performance monitoring and metrics collection
- **MaxConcurrentOperations**: Sets the maximum number of concurrent repository operations

These settings provide fine-grained control over the semantic model loading behavior while maintaining optimal performance for different scenarios.
