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

    .PARAMETER MicrosoftFoundry
        A hashtable containing Foundry Models configuration including authentication
        and model deployment settings.

    .PARAMETER SemanticModelRepository
        A hashtable containing semantic model repository configuration including
        provider type and related settings.

    .OUTPUTS
        This function does not return output but creates a settings.json file in the project directory.

    .EXAMPLE
        $dbConfig = @{ connectionString = "Server=.;Database=Test"; schema = "dbo" }
        $foundryConfig = @{ Default = @{ Endpoint = "https://test.services.ai.azure.com/"; AuthenticationType = "ApiKey"; ApiKey = "test-key" } }
        $repoConfig = @{ provider = "LocalDisk" }
        Set-ProjectSettings -ProjectPath "C:\temp\project" -Database $dbConfig -MicrosoftFoundry $foundryConfig -SemanticModelRepository $repoConfig

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
        [hashtable]$MicrosoftFoundry,

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
        SettingsVersion         = "2.0.0"
        Database                = $Database
        DataDictionary          = @{
            ColumnTypeMapping = @()
        }
        SemanticModel          = @{
            PersistenceStrategy = $PersistenceStrategy
            MaxDegreeOfParallelism = 1
        }
        SemanticModelRepository = $SemanticModelRepository
        MicrosoftFoundry           = $MicrosoftFoundry
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

    .PARAMETER MicrosoftFoundryEndpoint
        The Foundry Models endpoint URL (e.g., https://<resource>.services.ai.azure.com/).

    .PARAMETER MicrosoftFoundryApiKey
        The API key for authenticating with Foundry Models service.

    .PARAMETER NoAzureMode
        Switch to enable no-azure mode, which uses mock/local configurations instead of Azure services.

    .PARAMETER ChatCompletionDeploymentName
        The deployment name for the chat completion model (default: 'gpt-5-2-chat').

    .PARAMETER EmbeddingDeploymentName
        The deployment name for the text embedding model (default: 'text-embedding-3-large').
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

    .PARAMETER DatabaseSchema
        The database schema to use for filtering database objects (optional, if not provided will get all schemas).

    .OUTPUTS
        This function does not return output but creates a settings.json file in the project directory.

    .EXAMPLE
        Set-TestProjectConfiguration -ProjectPath "C:\temp\project" -ConnectionString "Server=.;Database=Test" -MicrosoftFoundryEndpoint "https://test.services.ai.azure.com/" -MicrosoftFoundryApiKey "test-key"

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
        [string]$DatabaseSchema = 'SalesLT',

        [Parameter()]
        [string]$MicrosoftFoundryEndpoint,

        [Parameter()]
        [string]$MicrosoftFoundryApiKey,

        [Parameter()]
        [bool]$NoAzureMode = $false,

        [Parameter()]
        [string]$ChatCompletionDeploymentName = 'gpt-5-2-chat',

        [Parameter()]
        [string]$EmbeddingDeploymentName = 'text-embedding-3-large',

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

    # Add Schema if provided
    if (-not [string]::IsNullOrEmpty($DatabaseSchema)) {
        $dbConfig.Schema = $DatabaseSchema
    }

    # Build Foundry Models configuration
    $MicrosoftFoundryConfig = @{
        Default = @{
            AuthenticationType = ($NoAzureMode) ? 'ApiKey' : 'ApiKey'
            Endpoint = ($NoAzureMode) ? 'https://dummy.services.ai.azure.com/api/projects/test-project' : $MicrosoftFoundryEndpoint
            ApiKey = ($NoAzureMode) ? 'dummy-api-key' : $MicrosoftFoundryApiKey
        }
        ChatCompletion = @{
            DeploymentName = ($NoAzureMode) ? 'gpt-4o-mini' : $ChatCompletionDeploymentName
        }
        Embedding = @{
            DeploymentName = ($NoAzureMode) ? 'text-embedding-3-small' : $EmbeddingDeploymentName
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
    Set-ProjectSettings -ProjectPath $ProjectPath -Database $dbConfig -MicrosoftFoundry $MicrosoftFoundryConfig -SemanticModelRepository $repoConfig -PersistenceStrategy $PersistenceStrategy
}

<#
    .SYNOPSIS
        Resolves a value from a parameter, environment variable, or default.

    .DESCRIPTION
        This function checks the parameter value first, then the environment variable,
        and finally falls back to the default value.

    .PARAMETER ParameterValue
        The value passed as a parameter.

    .PARAMETER EnvironmentName
        The name of the environment variable to check.

    .PARAMETER DefaultValue
        The default value to return if neither parameter nor environment variable is set.

    .OUTPUTS
        Returns the resolved string value.

    .EXAMPLE
        $strategy = Get-ParameterOrEnvironment -ParameterValue '' -EnvironmentName 'PERSISTENCE_STRATEGY' -DefaultValue 'LocalDisk'
#>
function Get-ParameterOrEnvironment {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter()]
        [string]$ParameterValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$EnvironmentName,

        [Parameter()]
        [string]$DefaultValue = $null
    )

    if (-not [string]::IsNullOrEmpty($ParameterValue)) {
        return $ParameterValue
    }

    $envValue = Get-Item -Path "Env:$EnvironmentName" -ErrorAction SilentlyContinue
    if ($envValue -and -not [string]::IsNullOrEmpty($envValue.Value)) {
        return $envValue.Value
    }

    return $DefaultValue
}

<#
    .SYNOPSIS
        Collects environment variables into a hashtable for a given persistence strategy.

    .DESCRIPTION
        This function reads all relevant environment variables and returns them in a
        structured hashtable. The variables collected depend on the persistence strategy.

    .PARAMETER PersistenceStrategy
        The persistence strategy to collect environment variables for.
        Valid values: 'LocalDisk', 'AzureBlob', 'CosmosDb'.

    .PARAMETER TestFilter
        The test filter string, resolved from parameter or environment variable.

    .OUTPUTS
        Returns a hashtable with PersistenceStrategy, TestFilter, NoAzureMode, and
        Environment keys.

    .EXAMPLE
        $testConfig = Initialize-TestEnvironment -PersistenceStrategy 'LocalDisk'
#>
function Initialize-TestEnvironment {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter()]
        [ValidateSet('LocalDisk', 'AzureBlob', 'CosmosDb')]
        [string]$PersistenceStrategy,

        [Parameter()]
        [string]$TestFilter
    )

    $resolvedStrategy = Get-ParameterOrEnvironment -ParameterValue $PersistenceStrategy -EnvironmentName 'PERSISTENCE_STRATEGY' -DefaultValue 'LocalDisk'
    $resolvedFilter = Get-ParameterOrEnvironment -ParameterValue $TestFilter -EnvironmentName 'TEST_FILTER'

    $noAzureMode = ($resolvedFilter -and ($resolvedFilter.ToString().Trim().ToLower() -eq 'no-azure')) -or
                  ($env:NO_AZURE_MODE -and ($env:NO_AZURE_MODE.ToString().Trim().ToLower() -in @('true', '1', 'yes')))

    # Core environment variables (needed by all strategies)
    $environmentVars = @{
        SQL_CONNECTION_STRING              = $env:SQL_CONNECTION_STRING
        DATABASE_SCHEMA                    = $env:DATABASE_SCHEMA
        AZURE_AI_FOUNDRY_PROJECT_ENDPOINT  = $env:AZURE_AI_FOUNDRY_PROJECT_ENDPOINT
        AZURE_OPENAI_API_KEY               = $env:AZURE_OPENAI_API_KEY
        PERSISTENCE_STRATEGY               = $resolvedStrategy
    }

    # Add AzureBlob-specific variables
    if ($resolvedStrategy -eq 'AzureBlob' -or -not $PersistenceStrategy) {
        $environmentVars.AZURE_STORAGE_ACCOUNT_ENDPOINT = $env:SemanticModelRepository__AzureBlob__AccountEndpoint
        $environmentVars.AZURE_STORAGE_CONTAINER         = $env:SemanticModelRepository__AzureBlob__ContainerName
        $environmentVars.AZURE_STORAGE_BLOB_PREFIX       = $env:SemanticModelRepository__AzureBlob__BlobPrefix
    }

    # Add CosmosDb-specific variables
    if ($resolvedStrategy -eq 'CosmosDb' -or -not $PersistenceStrategy) {
        $environmentVars.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT  = $env:AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
        $environmentVars.AZURE_COSMOS_DB_DATABASE_NAME     = $env:AZURE_COSMOS_DB_DATABASE_NAME
        $environmentVars.AZURE_COSMOS_DB_MODELS_CONTAINER  = $env:AZURE_COSMOS_DB_MODELS_CONTAINER
        $environmentVars.AZURE_COSMOS_DB_ENTITIES_CONTAINER = $env:AZURE_COSMOS_DB_ENTITIES_CONTAINER
    }

    foreach ($varName in $environmentVars.Keys) {
        if ([string]::IsNullOrEmpty($environmentVars[$varName])) {
            Write-Verbose "Environment variable not set: $varName" -Verbose
        }
    }

    return @{
        PersistenceStrategy = $resolvedStrategy
        TestFilter          = $resolvedFilter
        NoAzureMode         = $noAzureMode
        Environment         = $environmentVars
    }
}

<#
    .SYNOPSIS
        Validates that required environment variables are present.

    .DESCRIPTION
        Checks that the required environment variables for the given persistence strategy
        are set. If NoAzureMode is active, missing variables are tolerated.

    .PARAMETER Environment
        A hashtable of environment variable names to values.

    .PARAMETER NoAzureMode
        When true, missing variables are logged but do not cause a failure.

    .PARAMETER PersistenceStrategy
        The persistence strategy, used to determine additional required variables.

    .OUTPUTS
        Returns $true if validation passes.

    .EXAMPLE
        Test-RequiredEnvironmentVariables -Environment $env -NoAzureMode $false -PersistenceStrategy 'AzureBlob'
#>
function Test-RequiredEnvironmentVariables {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [hashtable]$Environment,

        [Parameter()]
        [bool]$NoAzureMode = $false,

        [Parameter()]
        [ValidateSet('LocalDisk', 'AzureBlob', 'CosmosDb')]
        [string]$PersistenceStrategy = 'LocalDisk'
    )

    $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_AI_FOUNDRY_PROJECT_ENDPOINT', 'AZURE_OPENAI_API_KEY')
    switch ($PersistenceStrategy) {
        'AzureBlob' { $requiredVars += 'AZURE_STORAGE_ACCOUNT_ENDPOINT' }
        'CosmosDb'  { $requiredVars += 'AZURE_COSMOS_DB_ACCOUNT_ENDPOINT' }
    }

    $missingVars = @($requiredVars | Where-Object { [string]::IsNullOrEmpty($Environment[$_]) })

    if ($missingVars -and $missingVars.Count -gt 0) {
        if ($NoAzureMode) {
            Write-Verbose "NoAzure mode active: skipping required env var enforcement" -Verbose
            return $true
        } else {
            Write-Warning "Missing required environment variables: $($missingVars -join ', ')"
            throw "Missing required environment variables: $($missingVars -join ', ')"
        }
    }

    return $true
}

<#
    .SYNOPSIS
        Creates a temporary workspace directory structure and validates the console app path.

    .DESCRIPTION
        This function prepares a clean test workspace with subdirectories for projects
        and validates that the console application executable exists.

    .PARAMETER TestDriveRoot
        The root directory for the test drive (typically Pester's $TestDrive).

    .PARAMETER ConsoleAppPath
        The path to the console application executable.

    .PARAMETER TempDirPrefix
        An optional prefix for the temp directory name when TestDrive is unavailable.
        Defaults to 'genaidb-integration-test'.

    .OUTPUTS
        Returns a hashtable with TestWorkspace, BaseProjectPath, and ConsoleAppPath.

    .EXAMPLE
        $workspace = Initialize-TestWorkspace -TestDriveRoot $TestDrive -ConsoleAppPath './app.exe'
#>
function Initialize-TestWorkspace {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$TestDriveRoot,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsoleAppPath,

        [Parameter()]
        [string]$TempDirPrefix = 'genaidb-integration-test'
    )

    if (-not (Test-Path -LiteralPath $TestDriveRoot)) {
        $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("$TempDirPrefix-" + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
        $TestDriveRoot = $tempRoot
    }

    $testWorkspacePath = Join-Path -Path $TestDriveRoot -ChildPath 'workspace'
    New-Item -ItemType Directory -Path $testWorkspacePath -Force | Out-Null

    $baseProjectPath = Join-Path -Path $testWorkspacePath -ChildPath 'projects'
    New-Item -ItemType Directory -Path $baseProjectPath -Force | Out-Null

    if (-not (Test-Path -Path $ConsoleAppPath)) {
        throw "Console application not found at: $ConsoleAppPath"
    }

    if (-not $IsWindows) {
        & chmod +x $ConsoleAppPath 2>&1 | Out-Null
    }

    return @{
        TestWorkspace   = (Get-Item -LiteralPath $testWorkspacePath)
        BaseProjectPath = $baseProjectPath
        ConsoleAppPath  = $ConsoleAppPath
    }
}

<#
    .SYNOPSIS
        Resolves the console application path from environment or default.

    .DESCRIPTION
        Returns the path to the console application executable, checking the
        CONSOLE_APP_PATH environment variable first, then falling back to the
        default build output path.

    .OUTPUTS
        Returns the resolved console application path string.

    .EXAMPLE
        $appPath = Resolve-ConsoleAppPath
#>
function Resolve-ConsoleAppPath {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    if ($env:CONSOLE_APP_PATH -and -not [string]::IsNullOrEmpty($env:CONSOLE_APP_PATH)) {
        return $env:CONSOLE_APP_PATH
    }

    return "./src/GenAIDBExplorer/GenAIDBExplorer.Console/bin/Debug/net10.0/GenAIDBExplorer.Console.exe"
}

Export-ModuleMember -Function Initialize-TestProject, Set-ProjectSettings, Invoke-ConsoleCommand, New-TestDataDictionary, Set-TestProjectConfiguration, Get-ParameterOrEnvironment, Initialize-TestEnvironment, Test-RequiredEnvironmentVariables, Initialize-TestWorkspace, Resolve-ConsoleAppPath
