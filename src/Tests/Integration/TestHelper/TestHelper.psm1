<#
    Test Helper module for GenAI Database Explorer Console Integration Tests

    This module provides fixture helper functions consumed by the Pester integration
    test suite. Functions are parameter-driven and do not read environment variables
    directly. The test script collects environment values and passes them into these
    helpers.
#>

#Requires -Version 7

<#
    .SYNOPSIS
        Executes the console application and captures output and exit code.

    .DESCRIPTION
        This function executes a console application with specified arguments and returns
        the output, exit code, and command line for testing purposes.

    .PARAMETER ConsoleApp
        The path to the console application executable.

    .PARAMETER Arguments
        The arguments to pass to the console application.

    .OUTPUTS
        Returns a hashtable with Output, ExitCode, and Command properties.

    .EXAMPLE
        $result = Invoke-ConsoleCommand -ConsoleApp ".\app.exe" -Arguments @('--help')
        Write-Host "Exit Code: $($result.ExitCode)"

    .NOTES
        This function is designed for test scenarios and captures both stdout and stderr.
#>
function Invoke-ConsoleCommand {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsoleApp,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [string[]]$Arguments
    )

    $commandLine = "$ConsoleApp $($Arguments -join ' ')"
    Write-Verbose "Executing: $commandLine" -Verbose

    $output = & $ConsoleApp @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    return @{ Output = $output; ExitCode = $exitCode; Command = $commandLine }
}

<#
    .SYNOPSIS
        Initializes a new test project by invoking the console app 'init-project' command.

    .DESCRIPTION
        This function creates a new test project directory and initializes it using the
        GenAI Database Explorer console application's init-project command.

    .PARAMETER ProjectPath
        The filesystem path where the new test project should be created.

    .PARAMETER ConsoleApp
        The path to the console application executable.

    .OUTPUTS
        Returns a hashtable with InitResult (console output array) and ExitCode properties.

    .EXAMPLE
        $result = Initialize-TestProject -ProjectPath "C:\temp\test" -ConsoleApp ".\app.exe"
        if ($result.ExitCode -eq 0) {
            Write-Host "Project initialized successfully"
        }

    .NOTES
        This function will create the project directory if it doesn't exist.
#>
function Initialize-TestProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsoleApp
    )

    if (-not (Test-Path -Path $ProjectPath)) {
        New-Item -ItemType Directory -Path $ProjectPath -Force | Out-Null
    }

    $result = Invoke-ConsoleCommand -ConsoleApp $ConsoleApp -Arguments @('init-project', '--project', $ProjectPath)
    return @{
        InitResult = $result.Output
        ExitCode = $result.ExitCode
    }
}

<#
    .SYNOPSIS
        Creates a simple JSON data dictionary used by tests.

    .DESCRIPTION
        This function generates a JSON data dictionary file with the specified database
        object information for use in integration tests of the data dictionary functionality.

    .PARAMETER DictionaryPath
        The path where the dictionary file should be created.

    .PARAMETER ObjectType
        The type of database object (e.g., 'table', 'view').

    .PARAMETER SchemaName
        The schema name for the database object.

    .PARAMETER ObjectName
        The name of the database object.

    .PARAMETER Description
        The description for the database object.

    .OUTPUTS
        This function does not return output but creates a JSON file at the specified path.

    .EXAMPLE
        New-TestDataDictionary -DictionaryPath "C:\temp\dict.json" -ObjectType "table" -SchemaName "dbo" -ObjectName "Customer" -Description "Customer information table"

    .NOTES
        This function creates the parent directory if it doesn't exist and generates
        a standardized JSON structure that matches the expected data dictionary format.
#>
function New-TestDataDictionary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$DictionaryPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ObjectType,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$SchemaName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ObjectName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Description
    )

    $dir = Split-Path -Path $DictionaryPath -Parent
    if (-not (Test-Path -Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $sample = @{
        objectType  = $ObjectType
        schemaName  = $SchemaName
        objectName  = $ObjectName
        description = $Description
        columns     = @(
            @{
                name        = 'ID'
                description = 'Unique identifier'
            }
        )
    }

    $sample | ConvertTo-Json -Depth 5 | Set-Content -Path $DictionaryPath
    Write-Verbose "Created test data dictionary at: $DictionaryPath" -Verbose
}

<#
    .SYNOPSIS
        Writes a minimal settings.json for a test project from provided values.

    .DESCRIPTION
        This function creates a JSON settings configuration file for the test project
        with the minimum required settings including database connection, OpenAI service
        configuration, and semantic model repository settings.

    .PARAMETER ProjectPath
        The path to the test project directory where settings.json will be created.

    .PARAMETER Database
        A hashtable containing database configuration including connectionString,
        schema, and other database-related settings.

    .PARAMETER OpenAIService
        A hashtable containing OpenAI service configuration including authentication
        and model deployment settings.

    .PARAMETER SemanticModelRepository
        A hashtable containing semantic model repository configuration including
        provider type and related settings.

    .OUTPUTS
        This function does not return output but creates a settings.json file in the project directory.

    .EXAMPLE
        $dbConfig = @{ connectionString = "Server=.;Database=Test"; schema = "dbo" }
        $aiConfig = @{ authType = "Key"; apiKey = "test-key" }
        $repoConfig = @{ provider = "LocalDisk" }
        Set-ProjectSettings -ProjectPath "C:\temp\project" -Database $dbConfig -OpenAIService $aiConfig -SemanticModelRepository $repoConfig

    .NOTES
        This function creates a complete settings.json file with all required sections
        for the GenAI Database Explorer project configuration.
#>
function Set-ProjectSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [hashtable]$Database,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [hashtable]$OpenAIService,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [hashtable]$SemanticModelRepository,

        [Parameter()]
        [ValidateSet('LocalDisk', 'AzureBlob', 'CosmosDb')]
        [string]$PersistenceStrategy = 'LocalDisk'
    )

    if (-not (Test-Path -Path $ProjectPath)) {
        New-Item -ItemType Directory -Path $ProjectPath -Force | Out-Null
    }

    $settings = [ordered]@{
        SettingsVersion         = "1.0.0"
        Database                = $Database
        DataDictionary          = @{
            ColumnTypeMapping = @()
        }
        SemanticModel          = @{
            PersistenceStrategy = $PersistenceStrategy
            MaxDegreeOfParallelism = 1
        }
        SemanticModelRepository = $SemanticModelRepository
        OpenAIService           = $OpenAIService
        VectorIndex            = @{
            Provider = 'Auto'
            CollectionName = 'genaide-entities'
            EmbeddingServiceId = 'Embeddings'
            AllowedForRepository = @('LocalDisk', 'AzureBlob', 'CosmosDb')
            Hybrid = @{ Enabled = $false }
        }
    }

    $settingsPath = Join-Path -Path $ProjectPath -ChildPath 'settings.json'
    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $settingsPath
    Write-Verbose "Created project settings at: $settingsPath" -Verbose
}

<#
    .SYNOPSIS
        High-level helper that prepares settings.json for a test project.

    .DESCRIPTION
        This function is a convenience wrapper that creates a complete project settings
        configuration by building the required hashtables and calling Set-ProjectSettings.
        It supports multiple authentication modes and persistence strategies.

    .PARAMETER ProjectPath
        The path to the test project directory where settings.json will be created.

    .PARAMETER ConnectionString
        The database connection string for connecting to the test database.

    .PARAMETER AzureOpenAIEndpoint
        The Azure OpenAI service endpoint URL.

    .PARAMETER AzureOpenAIApiKey
        The API key for authenticating with Azure OpenAI service.

    .PARAMETER NoAzureMode
        Switch to enable no-azure mode, which uses mock/local configurations instead of Azure services.

    .PARAMETER ChatCompletionDeploymentId
        The deployment ID for the chat completion model (default: 'gpt-4-1').

    .PARAMETER ChatCompletionStructuredDeploymentId
        The deployment ID for structured chat completion model (default: 'gpt-4-1-mini').

    .PARAMETER EmbeddingDeploymentId
        The deployment ID for the text embedding model (default: 'text-embedding-ada-002').
    .PARAMETER PersistenceStrategy
        The persistence strategy for semantic model storage. Valid values: 'LocalDisk', 'AzureBlob', 'CosmosDb'.

    .PARAMETER AzureStorageAccountEndpoint
        The Azure Storage account endpoint (required for AzureBlob persistence).

    .PARAMETER AzureStorageContainer
        The Azure Storage container name (optional, defaults to 'semantic-models').

    .PARAMETER AzureStorageBlobPrefix
        The blob prefix for Azure Storage (optional).

    .PARAMETER AzureCosmosDbAccountEndpoint
        The Azure Cosmos DB account endpoint (required for CosmosDb persistence).

    .PARAMETER AzureCosmosDbDatabaseName
        The Cosmos DB database name (optional, defaults to 'SemanticModels').

    .PARAMETER AzureCosmosDbModelsContainer
        The Cosmos DB models container name (optional, defaults to 'Models').

    .PARAMETER AzureCosmosDbEntitiesContainer
        The Cosmos DB entities container name (optional, defaults to 'ModelEntities').

    .OUTPUTS
        This function does not return output but creates a settings.json file in the project directory.

    .EXAMPLE
        Set-TestProjectConfiguration -ProjectPath "C:\temp\project" -ConnectionString "Server=.;Database=Test" -AzureOpenAIEndpoint "https://test.openai.azure.com" -AzureOpenAIApiKey "test-key"

    .EXAMPLE
        Set-TestProjectConfiguration -ProjectPath "C:\temp\project" -ConnectionString "Server=.;Database=Test" -NoAzureMode -PersistenceStrategy "LocalDisk"

    .NOTES
        This function builds the required configuration hashtables and delegates to Set-ProjectSettings
        for the actual file creation. It handles different persistence strategies and authentication modes.
#>
function Set-TestProjectConfiguration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ProjectPath,

        [Parameter()]
        [string]$ConnectionString = 'Server=dummy;Database=TestDB;Trusted_Connection=true;',

        [Parameter()]
        [string]$AzureOpenAIEndpoint,

        [Parameter()]
        [string]$AzureOpenAIApiKey,

        [Parameter()]
        [bool]$NoAzureMode = $false,

        [Parameter()]
        [string]$ChatCompletionDeploymentId = 'gpt-4-1',

        [Parameter()]
        [string]$ChatCompletionStructuredDeploymentId = 'gpt-4-1-mini',

        [Parameter()]
        [string]$EmbeddingDeploymentId = 'text-embedding-ada-002',

        [Parameter()]
        [ValidateSet('LocalDisk', 'AzureBlob', 'CosmosDb')]
        [string]$PersistenceStrategy = 'LocalDisk',

        [Parameter()]
        [string]$AzureStorageAccountEndpoint,

        [Parameter()]
        [string]$AzureStorageContainer,

        [Parameter()]
        [string]$AzureStorageBlobPrefix,

        [Parameter()]
        [string]$AzureCosmosDbAccountEndpoint,

        [Parameter()]
        [string]$AzureCosmosDbDatabaseName,

        [Parameter()]
        [string]$AzureCosmosDbModelsContainer,

        [Parameter()]
        [string]$AzureCosmosDbEntitiesContainer
    )

    # Normalize connection string and derive database name
    $normalizedConnectionString = ([string]::IsNullOrEmpty($ConnectionString)) ? 'Server=dummy;Database=TestDB;Trusted_Connection=true;' : $ConnectionString
    $dbNameMatch = [regex]::Match($normalizedConnectionString, '(?i)(?:Initial\s*Catalog|Database)\s*=\s*([^;]+)')
    $databaseName = $dbNameMatch.Success ? $dbNameMatch.Groups[1].Value.Trim() : 'TestDB'

    $dbConfig = @{
        Name = $databaseName
        ConnectionString = $normalizedConnectionString
    }

    # Build OpenAI service configuration
    $openAIConfig = @{
        Default = @{
            ServiceType = ($NoAzureMode) ? 'OpenAI' : 'AzureOpenAI'
            AzureOpenAIEndpoint = ($NoAzureMode) ? $null : $AzureOpenAIEndpoint
            AzureOpenAIKey = ($NoAzureMode) ? $null : $AzureOpenAIApiKey
            OpenAIKey = ($NoAzureMode) ? 'dummy-openai-key' : $null
        }
        ChatCompletion = @{
            AzureOpenAIDeploymentId = ($NoAzureMode) ? $null : $ChatCompletionDeploymentId
            ModelId = ($NoAzureMode) ? 'gpt-4o-mini' : $null
        }
        ChatCompletionStructured = @{
            AzureOpenAIDeploymentId = ($NoAzureMode) ? $null : $ChatCompletionStructuredDeploymentId
            ModelId = ($NoAzureMode) ? 'gpt-4o-mini' : $null
        }
        Embedding = @{
            AzureOpenAIDeploymentId = ($NoAzureMode) ? $null : $EmbeddingDeploymentId
            ModelId = ($NoAzureMode) ? 'text-embedding-3-small' : $null
        }
    }

    # Build semantic model repository configuration based on persistence strategy
    $repoConfig = @{}
    switch ($PersistenceStrategy) {
        'LocalDisk' {
            $repoConfig.LocalDisk = @{
                Directory = 'SemanticModel'
            }
        }
        'AzureBlob' {
            $repoConfig.AzureBlob = @{
                AccountEndpoint = ($NoAzureMode -or [string]::IsNullOrEmpty($AzureStorageAccountEndpoint)) ? 'https://test.blob.core.windows.net' : $AzureStorageAccountEndpoint
                ContainerName   = ([string]::IsNullOrEmpty($AzureStorageContainer)) ? 'semantic-models' : $AzureStorageContainer
                BlobPrefix      = $AzureStorageBlobPrefix
            }
        }
        'CosmosDb' {
            $repoConfig.CosmosDb = @{
                AccountEndpoint        = ($NoAzureMode -or [string]::IsNullOrEmpty($AzureCosmosDbAccountEndpoint)) ? 'https://test.documents.azure.com:443/' : $AzureCosmosDbAccountEndpoint
                DatabaseName           = $AzureCosmosDbDatabaseName ?? 'SemanticModels'
                ModelsContainerName    = $AzureCosmosDbModelsContainer ?? 'Models'
                EntitiesContainerName  = $AzureCosmosDbEntitiesContainer ?? 'ModelEntities'
            }
        }
    }

    # Call the core settings function with properly structured configurations
    Set-ProjectSettings -ProjectPath $ProjectPath -Database $dbConfig -OpenAIService $openAIConfig -SemanticModelRepository $repoConfig -PersistenceStrategy $PersistenceStrategy
}

Export-ModuleMember -Function Initialize-TestProject, Set-ProjectSettings, Invoke-ConsoleCommand, New-TestDataDictionary, Set-TestProjectConfiguration
