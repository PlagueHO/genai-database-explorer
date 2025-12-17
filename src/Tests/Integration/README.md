# Integration Tests

This directory contains integration tests for the GenAI Database Explorer Console Application.

## Test Structure

The integration tests are organized by **persistence strategy** to ensure each strategy is tested independently with its specific requirements and behaviors.

### Test Files

- **`Console.Integration.Tests.Common.ps1`** - Tests that **MUST** pass for ALL persistence strategies
  - Project initialization
  - CLI interface and help
  - Basic command validation
  - Universal behaviors

- **`Console.Integration.Tests.LocalDisk.ps1`** - Tests specific to **LocalDisk** strategy
  - File system operations
  - Local storage patterns
  - Direct file validation
  - Local disk-specific scenarios

- **`Console.Integration.Tests.AzureBlob.ps1`** - Tests specific to **AzureBlob** strategy
  - Azure Blob Storage operations
  - Blob-based persistence
  - Azure Storage authentication
  - Blob prefix handling

- **`Console.Integration.Tests.CosmosDb.ps1`** - Tests specific to **CosmosDb** strategy
  - Azure Cosmos DB operations
  - Document-based persistence
  - Dual-container architecture (Models + Entities)
  - Hierarchical partition key support

## Why Separate Test Files?

Each persistence strategy has **fundamentally different** characteristics:

| Aspect | LocalDisk | AzureBlob | CosmosDb |
|--------|-----------|-----------|----------|
| Storage | File system | Blob storage | Document database |
| Verification | Direct file access | Blob API calls | Query containers |
| Auth | None | Managed Identity/SAS | Managed Identity/API Key |
| Structure | Folder hierarchy | Blob prefix hierarchy | Containers + documents |
| Vectors | Files in subfolder | Separate blobs | Separate container |

Combining these into one test file with conditional logic would:

- Hide implementation gaps behind skip conditions
- Make tests harder to maintain
- Obscure what each strategy actually supports
- Create false confidence in test coverage

## Running Tests

### Locally

```powershell
# Run all tests for a specific strategy
$env:PERSISTENCE_STRATEGY = 'LocalDisk'
Invoke-Pester -Path './src/Tests/Integration/Console.Integration.Tests.Common.ps1'
Invoke-Pester -Path './src/Tests/Integration/Console.Integration.Tests.LocalDisk.ps1'

# Run only common tests
Invoke-Pester -Path './src/Tests/Integration/Console.Integration.Tests.Common.ps1'

# Run with specific configuration
$env:SQL_CONNECTION_STRING = 'Server=...;Database=...;'
$env:AZURE_OPENAI_ENDPOINT = 'https://....cognitiveservices.azure.com/'
$env:AZURE_OPENAI_API_KEY = '...'
Invoke-Pester -Path './src/Tests/Integration/Console.Integration.Tests.LocalDisk.ps1'
```

### In CI/CD

The GitHub Actions workflow automatically:

1. Runs **Common** tests for all strategies
2. Runs **strategy-specific** tests based on the matrix
3. Publishes separate results for each strategy

See `.github/workflows/console-integration-tests.yml` for details.

## Required Environment Variables

### Common (All Strategies)

- `SQL_CONNECTION_STRING` - Connection to test database
- `AZURE_OPENAI_ENDPOINT` - Azure OpenAI service endpoint  
- `AZURE_OPENAI_API_KEY` - Azure OpenAI API key
- `PERSISTENCE_STRATEGY` - Strategy being tested
- `DATABASE_SCHEMA` - Database schema filter (optional)
- `CONSOLE_APP_PATH` - Path to published console app

### AzureBlob Strategy

- `SemanticModelRepository__AzureBlob__AccountEndpoint` - Storage account endpoint
- `SemanticModelRepository__AzureBlob__ContainerName` - Container name (optional)
- `SemanticModelRepository__AzureBlob__BlobPrefix` - Blob prefix (optional)

### CosmosDb Strategy

- `AZURE_COSMOS_DB_ACCOUNT_ENDPOINT` - Cosmos DB account endpoint
- `AZURE_COSMOS_DB_DATABASE_NAME` - Database name (optional, defaults to SemanticModels)
- `AZURE_COSMOS_DB_MODELS_CONTAINER` - Models container (optional, defaults to Models)
- `AZURE_COSMOS_DB_ENTITIES_CONTAINER` - Entities container (optional, defaults to ModelEntities)

## Test Helper Module

The `TestHelper` module provides shared functions for all test files:

- `Initialize-TestProject` - Creates and initializes a test project
- `Invoke-ConsoleCommand` - Executes console app and captures output
- `Set-TestProjectConfiguration` - Configures project settings.json
- `New-TestDataDictionary` - Creates test data dictionary files

See `TestHelper/TestHelper.psm1` for full documentation.

## Test Flow for Each Strategy

Each strategy-specific test file follows a consistent workflow that validates the complete lifecycle of semantic model operations.

### Common Tests Flow

Tests that run for **all strategies** before strategy-specific tests:

1. **Project Initialization**
   - `init-project` - Create new project with settings.json
   - Validate project structure creation
   - Verify settings.json contents

2. **CLI Interface**
   - `--help` - Verify help information displays
   - Invalid commands - Verify proper error handling

### LocalDisk Strategy Flow

Tests specific to local file system persistence:

1. **Database Schema Extraction**
   - `extract-model` - Extract schema from database
   - Verify `SemanticModel/semanticmodel.json` created on disk
   - Validate model name matches database
   - `extract-model --skipTables` - Test extraction options

2. **Data Dictionary Application**
   - `data-dictionary table` - Apply metadata from JSON files
   - Verify dictionary files read from local file system
   - Test `--sourcePathPattern`, `--schemaName`, `--name`, `--show` options

3. **Vector Generation**
   - `generate-vectors --dryRun` - Test vector generation planning
   - `generate-vectors table` - Generate and persist vectors for specific object
   - Verify `SemanticModel/Vectors/` directory created on disk
   - Test `--overwrite` option

4. **AI Enrichment**
   - `enrich-model` - Add AI-generated descriptions
   - Verify enriched model saved to local disk
   - Validate model file updated in place

5. **Model Display**
   - `show-object table` - Display table information from local model
   - Verify data retrieved from `SemanticModel/semanticmodel.json`

6. **Model Export**
   - `export-model` - Export to markdown format
   - Verify exported file created on local disk
   - `export-model --splitFiles` - Export to multiple files
   - Verify split file structure on disk

### AzureBlob Strategy Flow

Tests specific to Azure Blob Storage persistence:

1. **Database Schema Extraction**
   - `extract-model` - Extract and store in Azure Blob Storage
   - Verify model accessible via blob storage APIs
   - Test with configured blob prefix

2. **Vector Generation**
   - `generate-vectors --dryRun` - Test planning with blob storage
   - Verify vectors can be persisted to separate blobs

3. **AI Enrichment**
   - `enrich-model` - Enrich and update in blob storage
   - Verify model updated in Azure Blob Storage

4. **Model Display**
   - `show-object table` - Display from blob storage model
   - Verify blob retrieval and deserialization

5. **Model Export**
   - `export-model` - Export blob storage model to local file
   - Verify blob-to-file conversion

6. **Blob Storage Scenarios**
   - Test blob prefix configuration
   - Verify storage account endpoint handling

### CosmosDb Strategy Flow

Tests specific to Azure Cosmos DB document persistence:

1. **Database Schema Extraction**
   - `extract-model` - Extract and store in Cosmos DB
   - Verify model document created in Models container
   - Test dual-container architecture

2. **Vector Generation**
   - `generate-vectors --dryRun` - Test planning with Cosmos DB
   - `generate-vectors table` - Persist vectors to Entities container
   - Verify separation between model and entity documents

3. **AI Enrichment**
   - `enrich-model` - Enrich and update Cosmos DB document
   - Verify model document updated in Models container

4. **Model Display**
   - `show-object table` - Display from Cosmos DB model
   - Verify document query and retrieval

5. **Model Export**
   - `export-model` - Export Cosmos DB model to local file
   - Verify document-to-file conversion

6. **Cosmos DB Scenarios**
   - Test dual-container configuration (Models + Entities)
   - Verify hierarchical partition key support
   - Test database and container name configuration

### Test Execution Order

For each strategy, the workflow runs tests in **chronological order** to simulate real-world usage:

```
1. Initialize Project → 2. Extract Model → 3. Apply Dictionaries → 
4. Generate Vectors → 5. Enrich with AI → 6. Display Objects → 7. Export Model
```

Tests use **incremental state** where later tests depend on earlier operations succeeding. If extraction fails, subsequent tests are marked as **Skipped** or **Inconclusive** rather than failing.

## Adding New Tests

1. **If the test applies to ALL strategies**: Add to `Console.Integration.Tests.Common.ps1`
2. **If the test is strategy-specific**: Add to the appropriate strategy file
3. **If unsure**: Add to the strategy file where you're implementing the feature first, then migrate to Common if it proves universal
4. **Maintain test flow order**: Insert new tests in the logical workflow sequence (e.g., new enrichment tests go between extraction and display)
