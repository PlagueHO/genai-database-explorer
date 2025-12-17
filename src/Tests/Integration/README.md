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

## Adding New Tests

1. **If the test applies to ALL strategies**: Add to `Console.Integration.Tests.Common.ps1`
2. **If the test is strategy-specific**: Add to the appropriate strategy file
3. **If unsure**: Add to the strategy file where you're implementing the feature first, then migrate to Common if it proves universal

## Migrating from Old Structure

The previous `Console.Integration.Tests.ps1` combined all strategies with conditional `-Skip` logic. This has been refactored into the current structure for:
- **Clarity**: Each file clearly shows what that strategy supports
- **Maintainability**: Changes to one strategy don't affect others
- **Reliability**: No hidden skips or conditional logic
- **Documentation**: The file structure itself documents capabilities
