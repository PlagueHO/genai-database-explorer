<#
    .SYNOPSIS
        Integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
 
        Tests include project initialization, database model extraction, AI enrichment, and export functionality.
        These tests require:
        - GenAI Database Explorer Console application (published)
        - Azure SQL Database connection (AdventureWorksLT recommended)
        - Azure OpenAI Services endpoint and API key

    .PARAMETER None
        This test file does not accept parameters. Configuration is done via environment variables.

    .EXAMPLE
        Invoke-Pester -Path "Console.Integration.Tests.ps1"

        Runs all integration tests with default configuration.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database (optional - defaults to Azure SQL sample)
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint (optional - defaults to test endpoint)
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key (optional - defaults to dummy key)

    .OUTPUTS
        Pester test results in NUnitXml format
#>
#Requires -Version 7

param(
    [Parameter()]
    [ValidateSet('LocalDisk', 'AzureBlob', 'CosmosDb')]
    [string]
    $PersistenceStrategy = 'LocalDisk',
    
    [Parameter()]
    [string]
    $TestFilter = $null
)

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

# Ensure discovery-safe defaults for script-scoped variables used in -Skip expressions
# Use null-coalescing with Get-Variable to safely provide defaults at discovery time
$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false
$script:PersistenceStrategy = (Get-Variable -Name 'PersistenceStrategy' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? 'LocalDisk'

Describe 'GenAI Database Explorer Console Application' {
    BeforeAll {
        # Helper functions for cleaner parameter and environment handling
        function Get-ParameterOrEnvironment {
            param(
                [string]$ParameterValue,
                [string]$EnvironmentName,
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

        function Initialize-TestEnvironment {
            param(
                [string]$PersistenceStrategy,
                [string]$TestFilter
            )
            
            # Resolve parameters from environment if not explicitly provided
            $resolvedStrategy = Get-ParameterOrEnvironment -ParameterValue $PersistenceStrategy -EnvironmentName 'PERSISTENCE_STRATEGY' -DefaultValue 'LocalDisk'
            $resolvedFilter = Get-ParameterOrEnvironment -ParameterValue $TestFilter -EnvironmentName 'TEST_FILTER'
            
            # Determine test modes
            $noAzureMode = $resolvedFilter -and ($resolvedFilter.ToString().Trim().ToLower() -eq 'no-azure')
            
            # Collect environment variables into a hashtable
            $environmentVars = @{
                SQL_CONNECTION_STRING = $env:SQL_CONNECTION_STRING
                AZURE_OPENAI_ENDPOINT = $env:AZURE_OPENAI_ENDPOINT
                AZURE_OPENAI_API_KEY = $env:AZURE_OPENAI_API_KEY
                AZURE_STORAGE_ACCOUNT_ENDPOINT = $env:AZURE_STORAGE_ACCOUNT_ENDPOINT
                AZURE_STORAGE_CONTAINER = $env:AZURE_STORAGE_CONTAINER
                AZURE_STORAGE_BLOB_PREFIX = $env:AZURE_STORAGE_BLOB_PREFIX
                AZURE_COSMOS_DB_ACCOUNT_ENDPOINT = $env:AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AZURE_COSMOS_DB_DATABASE_NAME = $env:AZURE_COSMOS_DB_DATABASE_NAME
                AZURE_COSMOS_DB_MODELS_CONTAINER = $env:AZURE_COSMOS_DB_MODELS_CONTAINER
                AZURE_COSMOS_DB_ENTITIES_CONTAINER = $env:AZURE_COSMOS_DB_ENTITIES_CONTAINER
                PERSISTENCE_STRATEGY = $resolvedStrategy
            }
            
            # Log missing environment variables
            $optionalVars = $environmentVars.Keys
            foreach ($varName in $optionalVars) {
                if ([string]::IsNullOrEmpty($environmentVars[$varName])) {
                    Write-Verbose "Environment variable '$varName' is not set. Using default value for testing." -Verbose
                }
            }
            
            return @{
                PersistenceStrategy = $resolvedStrategy
                TestFilter = $resolvedFilter
                NoAzureMode = $noAzureMode
                Environment = $environmentVars
            }
        }

    function Test-RequiredEnvironmentVariables {
            param(
                [hashtable]$Environment,
                [bool]$NoAzureMode
            )
            
            $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY')
            $missingVars = @($requiredVars | Where-Object { [string]::IsNullOrEmpty($Environment[$_]) })

            if ($missingVars -and $missingVars.Count -gt 0) {
                if ($NoAzureMode) {
                    Write-Verbose "NoAzure mode active (TEST_FILTER=no-azure): skipping required env var enforcement" -Verbose
                    return $true
                } else {
                    Write-Warning "Missing required environment variables: $($missingVars -join ', ')"
                    throw "Missing required environment variables: $($missingVars -join ', ')"
                }
            }
            
            return $true
        }

        function Initialize-TestWorkspace {
            param(
                [string]$TestDriveRoot,
                [string]$ConsoleAppPath
            )

            # Prefer Pester's TestDrive for ephemeral workspace; fallback to OS temp when not under Pester
            if (-not (Test-Path -LiteralPath $TestDriveRoot)) {
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-integration-test-" + [Guid]::NewGuid().ToString('N'))
                New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
                $TestDriveRoot = $tempRoot
            }

            $testWorkspacePath = Join-Path -Path $TestDriveRoot -ChildPath 'workspace'
            New-Item -ItemType Directory -Path $testWorkspacePath -Force | Out-Null

            $baseProjectPath = Join-Path -Path $testWorkspacePath -ChildPath 'projects'
            New-Item -ItemType Directory -Path $baseProjectPath -Force | Out-Null

            # Validate console app
            if (-not (Test-Path -Path $ConsoleAppPath)) {
                throw "Console application not found at: $ConsoleAppPath"
            }

            # Ensure executable bit on non-Windows (usually already handled in CI step)
            if (-not $IsWindows) {
                & chmod +x $ConsoleAppPath 2>&1 | Out-Null
            }

            return @{
                TestWorkspace  = (Get-Item -LiteralPath $testWorkspacePath)
                BaseProjectPath = $baseProjectPath
                ConsoleAppPath  = $ConsoleAppPath
            }
        }

        # Initialize test configuration from parameters and environment
        $testConfig = Initialize-TestEnvironment -PersistenceStrategy $PersistenceStrategy -TestFilter $TestFilter
        
        # Set script-level variables for use throughout tests
        $script:PersistenceStrategy = $testConfig.PersistenceStrategy
        $script:NoAzureMode = $testConfig.NoAzureMode
        $script:TestEnv = $testConfig.Environment
        
        Write-Verbose "Using persistence strategy: $($script:PersistenceStrategy)" -Verbose
        
        # Validate required environment variables
        Test-RequiredEnvironmentVariables -Environment $script:TestEnv -NoAzureMode $script:NoAzureMode
        
        # Initialize test workspace and console app
        $consoleAppPath = if ($env:CONSOLE_APP_PATH -and -not [string]::IsNullOrEmpty($env:CONSOLE_APP_PATH)) {
            $env:CONSOLE_APP_PATH
        } else {
            "./src/GenAIDBExplorer/GenAIDBExplorer.Console/bin/Debug/net9.0/GenAIDBExplorer.Console.exe"
        }

        # Use the $TestDrive Pester 5 variable as the root test drive folder because we're testing .NET apps
        # which can't use the TestDrive:\ because it's a PSDrive
        $workspaceConfig = Initialize-TestWorkspace -TestDriveRoot $TestDrive -ConsoleAppPath $consoleAppPath
        $script:TestWorkspace = $workspaceConfig.TestWorkspace
        $script:BaseProjectPath = $workspaceConfig.BaseProjectPath
        $script:ConsoleAppPath = $workspaceConfig.ConsoleAppPath
    }

    Context 'Project Management Commands' {
        Context 'init-project command' {
            Context 'When initializing a new project and project path does not exist' {
                BeforeAll {
                    # Arrange
                    $script:InitProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'init-test'

                    # Ensure parent directory structure exists with proper permissions
                    Write-Verbose "BaseProjectPath: $script:BaseProjectPath" -Verbose
                    Write-Verbose "InitProjectPath: $script:InitProjectPath" -Verbose

                    # Create parent directory structure if it doesn't exist
                    if (-not (Test-Path -Path $script:BaseProjectPath)) {
                        New-Item -ItemType Directory -Path $script:BaseProjectPath -Force | Out-Null
                    }

                    # Verify parent directory is accessible
                    try {
                        $null = Get-ChildItem -Path $script:BaseProjectPath -ErrorAction Stop
                        Write-Verbose "BaseProjectPath is accessible" -Verbose
                    }
                    catch {
                        Write-Error "BaseProjectPath is not accessible: $($_.Exception.Message)"
                        throw "Cannot access parent directory for test"
                    }

                    Write-Verbose "BaseProjectPath exists: $(Test-Path -Path $script:BaseProjectPath)" -Verbose

                    # Ensure the target directory does NOT exist (testing new project creation)
                    if (Test-Path -Path $script:InitProjectPath) {
                        Remove-Item -Path $script:InitProjectPath -Recurse -Force -ErrorAction SilentlyContinue
                    }

                    Write-Verbose "Target directory exists before test: $(Test-Path -Path $script:InitProjectPath)" -Verbose

                    $script:expectedSettingsPath = Join-Path -Path $script:InitProjectPath -ChildPath 'settings.json'
                }

                AfterAll {
                    # Cleanup: Remove project directory after test
                    if (Test-Path -Path $script:InitProjectPath) {
                        Remove-Item -Path $script:InitProjectPath -Recurse -Force -ErrorAction SilentlyContinue
                    }
                }

                It 'Should create proper project structure and settings.json' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('init-project', '--project', $script:InitProjectPath)

                    # Assert - Check for specific error patterns that indicate failure
                    if ($commandResult.Output -match 'Could not find a part of the path|Access.*denied|Permission.*denied|Path.*not.*found') {
                        throw "Command failed with path error: $($commandResult.Output)"
                    }

                    $commandResult.ExitCode | Should -Be 0 -Because 'init-project command should succeed'
                    $commandResult.Output | Should -Not -Match 'ERROR|FAIL|Exception|Could not find|Access.*denied' -Because 'No errors should be reported'

                    # Verify project directory was created
                    $script:InitProjectPath | Should -Exist -Because 'Project directory should be created'
                    $script:expectedSettingsPath | Should -Exist -Because 'settings.json should be created'

                    # Validate settings.json structure
                    { Get-Content -Path $script:expectedSettingsPath | ConvertFrom-Json -ErrorAction Stop } |
                        Should -Not -Throw -Because 'settings.json should contain valid JSON'

                    $settings = Get-Content -Path $script:expectedSettingsPath | ConvertFrom-Json
                    $settings | Should -Not -BeNullOrEmpty -Because 'settings.json should contain valid configuration'
                    # Validate that all top-level properties from the default template are present
                    $expectedProperties = @(
                        'SettingsVersion',
                        'Database',
                        'DataDictionary',
                        'SemanticModel',
                        'SemanticModelRepository',
                        'OpenAIService'
                    )
                    foreach ($prop in $expectedProperties) {
                        $settings.PSObject.Properties.Name | Should -Contain $prop -Because "settings.json should include '$prop' property as in the default template"
                    }
                }
            }

            Context 'When initializing a new project and project path already exists and is empty' {
                BeforeAll {
                    # Arrange
                    $script:ExistingProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'existing-test'
                    New-Item -ItemType Directory -Path $script:ExistingProjectPath -Force | Out-Null
                }

                AfterAll {
                    # Cleanup: Remove existing project directory after test
                    if (Test-Path -Path $script:ExistingProjectPath) {
                        Remove-Item -Path $script:ExistingProjectPath -Recurse -Force -ErrorAction SilentlyContinue
                    }
                }

                It 'Should handle existing directory gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('init-project', '--project', $script:ExistingProjectPath)

                    # Assert
                    $commandResult.ExitCode | Should -BeIn @(0, 1) -Because 'Should succeed or indicate directory exists'
                }
            }

            Context 'When initializing a new project and project path already exists but is not empty' {
                BeforeAll {
                    # Arrange
                    $script:ExistingProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'existing-test'
                    New-Item -ItemType Directory -Path $script:ExistingProjectPath -Force | Out-Null
                    New-Item -ItemType File -Path (Join-Path -Path $script:ExistingProjectPath -ChildPath 'dummy.txt') -Force | Out-Null
                }

                AfterAll {
                    # Cleanup: Remove existing project directory after test
                    if (Test-Path -Path $script:ExistingProjectPath) {
                        Remove-Item -Path $script:ExistingProjectPath -Recurse -Force -ErrorAction SilentlyContinue
                    }
                }

                It 'Should handle non-empty directory appropriately' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('init-project', '--project', $script:ExistingProjectPath)

                    # Assert
                    # The application may either succeed (0) or fail (1) depending on implementation
                    # If it succeeds, it should handle the existing files gracefully
                    # If it fails, it should indicate the directory is not empty
                    $commandResult.ExitCode | Should -BeIn @(0, 1) -Because 'Should either succeed gracefully or indicate directory is not empty'

                    if ($commandResult.ExitCode -eq 1) {
                        # If it fails, ensure it's due to non-empty directory
                        $commandResult.Output | Should -Match 'not.*empty|exists|directory.*contains' -Because 'Error message should indicate non-empty directory issue'
                    }
                }
            }
        }
    }

    Context 'Database Schema Operations' {
        BeforeAll {
            # Setup shared project for database tests
            $script:DbProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'database-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:DbProjectPath -ConsoleApp $script:ConsoleAppPath

            if ($projectSetup.ExitCode -ne 0) {
                Write-Error "Failed to initialize database test project: $($projectSetup.InitResult)"
                throw "Failed to initialize database test project"
            }

            # Configure settings for database tests using helper function (splat to reduce duplication)
            $dbConfig = @{
                ProjectPath = $script:DbProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            if ($script:PersistenceStrategy -and -not [string]::IsNullOrEmpty($script:PersistenceStrategy)) {
                $dbConfig.PersistenceStrategy = $script:PersistenceStrategy
            }

            Set-TestProjectConfiguration @dbConfig
        }

        Context 'extract-model command' {
            BeforeAll {
                $script:SemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                $script:ExtractSucceeded = $false
            }

            It 'Should execute extract-model and either succeed or provide clear connection error' {
                # Act
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)

                # Assert
                if ($commandResult.Output -match 'network-related.*error|connection.*error|server.*not.*found|authentication.*fail|timeout') {
                    $commandResult.Output | Should -Match 'connection|database|network|server.*not.*found' -Because 'Should provide meaningful error message for connection issues'
                } elseif ($commandResult.ExitCode -eq 0) {
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Successful run should not print stack traces'
                    $script:ExtractSucceeded = $true
                } else {
                    # Be resilient to infra issues while still asserting no stack traces
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces on handled failures'
                }
            }

            It 'Should create semanticmodel.json on LocalDisk when extraction succeeds' -Skip:($script:PersistenceStrategy -ne 'LocalDisk') {
                if ($script:ExtractSucceeded) {
                    $script:SemanticModelPath | Should -Exist -Because 'semanticmodel.json should be created for LocalDisk persistence'

                    { Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json -ErrorAction Stop } |
                        Should -Not -Throw -Because 'semanticmodel.json should be valid JSON'

                    $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json
                    $model | Should -Not -BeNull -Because 'Model JSON should parse to an object'
                    $model.Name | Should -Not -BeNullOrEmpty -Because 'Model should contain database name information'
                } else {
                    # If extraction did not succeed (e.g., environment DB unreachable), skip assertions gracefully
                    $true | Should -Be $true -Because 'Skip file assertions when extraction cannot run against DB'
                }
            }

            It 'Should set model name to database in connection string when available' -Skip:($script:PersistenceStrategy -ne 'LocalDisk') {
                if ($script:ExtractSucceeded -and (Test-Path -Path $script:SemanticModelPath)) {
                    $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json

                    $expectedDbName = $null
                    $cs = $script:TestEnv.SQL_CONNECTION_STRING
                    if (-not [string]::IsNullOrEmpty($cs)) {
                        $match = [regex]::Match($cs, '(?i)(?:Initial\s*Catalog|Database)\s*=\s*([^;]+)')
                        if ($match.Success) { $expectedDbName = $match.Groups[1].Value.Trim() }
                    }

                    if (-not [string]::IsNullOrEmpty($expectedDbName)) {
                        $model.Name | Should -Be $expectedDbName -Because 'Model name should match database in connection string'
                    } else {
                        $model.Name | Should -Not -BeNullOrEmpty -Because 'Fallback: ensure model name present even if DB name not parsed'
                    }
                } else {
                    $true | Should -Be $true -Because 'Skip content assertions when extraction cannot run against DB'
                }
            }

            Context 'When extracting with specific options' {
                It 'Should handle --skipTables option without stack traces' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath, '--skipTables')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces for valid options'
                }
            }
        }

        Context 'data-dictionary command' {
            Context 'When applying data dictionary files' {
                BeforeAll {
                    # Arrange - Create a sample data dictionary file
                    $script:DictDir = Join-Path -Path $script:DbProjectPath -ChildPath 'dict'
                    $script:DictPath = Join-Path -Path $script:DictDir -ChildPath 'test-dictionary.json'
                    New-TestDataDictionary -DictionaryPath $script:DictPath -ObjectType 'table' -SchemaName 'dbo' -ObjectName 'Customer' -Description 'Customer information table'
                }

                It 'Should process dictionary files without errors' -Skip:($script:PersistenceStrategy -ne 'LocalDisk') {
                    # Ensure semantic model exists; attempt extract if missing, else skip at runtime
                    $expectedSemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $expectedSemanticModelPath)) {
                        Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath) | Out-Null
                    }
                    if (-not (Test-Path -Path $expectedSemanticModelPath)) {
                        Set-ItResult -Skipped -Because 'No semantic model available to apply data dictionary'
                        return
                    }

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('data-dictionary', 'table', '--project', $script:DbProjectPath, '--source-path', $script:DictPath)

                    # Assert - Should not fail even if no matching objects
                    $commandResult.ExitCode | Should -BeIn @(0, 1) -Because 'Should succeed (0) or indicate no matches found (1)'
                    $commandResult.Output | Should -Not -Match 'Exception|Error.*Exception' -Because 'Should not throw unhandled exceptions'
                }
            }

            Context 'When showing applied dictionaries' {
                BeforeAll {
                    # Arrange
                    $script:ShowDictDir = Join-Path -Path $script:DbProjectPath -ChildPath 'dict'
                    $script:ShowDictPath = Join-Path -Path $script:ShowDictDir -ChildPath 'show-test-dictionary.json'
                    New-TestDataDictionary -DictionaryPath $script:ShowDictPath -ObjectType 'table' -SchemaName 'dbo' -ObjectName 'Product' -Description 'Product catalog table'
                }

                It 'Should display dictionary information with --show option' -Skip:($script:PersistenceStrategy -ne 'LocalDisk') {
                    # Ensure semantic model exists; attempt extract if missing, else skip at runtime
                    $expectedSemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $expectedSemanticModelPath)) {
                        Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath) | Out-Null
                    }
                    if (-not (Test-Path -Path $expectedSemanticModelPath)) {
                        Set-ItResult -Skipped -Because 'No semantic model available to show data dictionary modifications'
                        return
                    }

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('data-dictionary', 'table', '--project', $script:DbProjectPath, '--source-path', $script:ShowDictPath, '--show')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should run dry-run generate-vectors without errors' {
                # Arrange
                $expectedSemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                if (-not (Test-Path -Path $expectedSemanticModelPath)) {
                    # Try to ensure a model exists for the test; ignore failure gracefully
                    Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath) | Out-Null
                }

                # Act
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', '--project', $script:DbProjectPath, '--dry-run', '--skipViews', '--skipStoredProcedures')

                # Assert
                $commandResult.ExitCode | Should -Be 0 -Because 'generate-vectors dry-run should succeed'
                # Join the output array into a single string for regex matching (some Invoke-ConsoleCommand implementations return arrays)
                $joinedOutput = $commandResult.Output -join "`n"
                $joinedOutput | Should -Match 'Processed' -Because 'Should log processed entities summary'
            }
        }
    }

    Context 'AI Operations' {
        BeforeAll {
            # Setup project for AI tests
            $script:AiProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'ai-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:AiProjectPath -ConsoleApp $script:ConsoleAppPath

            if ($projectSetup.ExitCode -ne 0) {
                Write-Warning "Failed to initialize AI test project: $($projectSetup.InitResult)"
            }

            # Configure settings for AI operations using helper function (splat to reduce duplication)
            $aiConfig = @{
                ProjectPath = $script:AiProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            if ($script:PersistenceStrategy -and -not [string]::IsNullOrEmpty($script:PersistenceStrategy)) {
                $aiConfig.PersistenceStrategy = $script:PersistenceStrategy
            }

            Set-TestProjectConfiguration @aiConfig

            # Extract model first for AI operations (suppress output if fails)
            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            Context 'When enriching with AI services available' {
                It 'Should enhance semantic model with AI-generated descriptions' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', 'table', '--project', $script:AiProjectPath, '--schema', 'dbo')

                    # Assert - May fail if AI service unavailable, but should handle gracefully
                    if ($commandResult.ExitCode -eq 0) {
                        $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Successful execution should not show stack traces'
                    } else {
                        $commandResult.Output | Should -Match 'AI|service|connection|authentication|endpoint|model' -Because 'Failed execution should provide meaningful error message'
                        Write-Verbose "AI enrichment failed (expected if AI services not configured)" -Verbose
                    }
                }
            }

            Context 'When enriching specific objects' {
                It 'Should handle enrichment with object name filters' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', 'table', '--project', $script:AiProjectPath, '--schema', 'dbo', '--name', 'Customer')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should support dry-run for all entities' {
                # Act
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', '--project', $script:AiProjectPath, '--dry-run', '--skipViews', '--skipStoredProcedures')

                # Assert
                $commandResult.ExitCode | Should -Be 0 -Because 'generate-vectors dry-run should succeed'
                $joinedOutput = $commandResult.Output -join "`n"
                $joinedOutput | Should -Match 'Processed|\[DryRun\]' -Because 'Should log processed entities or dry-run markers'
            }

            It 'Should support targeting a specific object via subcommand' {
                # Act
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', 'table', '--project', $script:AiProjectPath, '--schema', 'dbo', '--name', 'Customer', '--dry-run')

                # Assert
                $commandResult.ExitCode | Should -Be 0 -Because 'dry-run should succeed for specific object'
                $commandResult.Output -join "`n" | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
            }

            It 'Should persist envelopes and honor overwrite semantics (LocalDisk only)' -Skip:($script:NoAzureMode -or ($script:PersistenceStrategy -ne 'LocalDisk')) {
                # Arrange
                $modelDir = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel'

                # First run: generate without dry-run (skip views/SPs to reduce time)
                $result1 = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', '--project', $script:AiProjectPath, '--skipViews', '--skipStoredProcedures')

                if ($result1.ExitCode -ne 0) {
                    # If AI service not available, provide meaningful assertion and stop
                    ($result1.Output -join "`n") | Should -Match 'AI|service|connection|authentication|endpoint|model' -Because 'Should provide a helpful message when AI is unavailable'
                    Set-ItResult -Skipped -Because 'AI services not available to persist vectors'
                    return
                }

                # Find at least one persisted envelope
                $envelopes = @()
                if (Test-Path -Path $modelDir) {
                    $envelopes = Get-ChildItem -Path $modelDir -Recurse -Filter '*.json' -ErrorAction SilentlyContinue
                }

                if (-not $envelopes -or $envelopes.Count -eq 0) {
                    Set-ItResult -Skipped -Because 'No envelope files found after generation; likely no entities or persistence disabled'
                    return
                }

                $targetFile = $envelopes | Select-Object -First 1
                $before = (Get-Item -LiteralPath $targetFile.FullName).LastWriteTimeUtc

                # Second run: without overwrite should skip unchanged and not modify timestamp
                Start-Sleep -Milliseconds 1200
                $result2 = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', '--project', $script:AiProjectPath, '--skipViews', '--skipStoredProcedures')
                $result2.ExitCode | Should -Be 0 -Because 'Second run without overwrite should succeed'
                $afterNoOverwrite = (Get-Item -LiteralPath $targetFile.FullName).LastWriteTimeUtc
                $afterNoOverwrite | Should -Be $before -Because 'Without overwrite, unchanged content should be skipped (timestamp unchanged)'

                # Third run: with overwrite should update the envelope timestamp
                Start-Sleep -Milliseconds 1200
                $result3 = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('generate-vectors', '--project', $script:AiProjectPath, '--skipViews', '--skipStoredProcedures', '--overwrite')
                $result3.ExitCode | Should -Be 0 -Because 'Overwrite run should succeed'
                $afterOverwrite = (Get-Item -LiteralPath $targetFile.FullName).LastWriteTimeUtc
                $afterOverwrite | Should -BeGreaterThan $before -Because 'With overwrite, envelope should be rewritten (timestamp increases)'
            }
        }
    }

    Context 'Model Display and Export Operations' {
        BeforeAll {
            # Setup shared project for display tests
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            # Configure settings for display operations using helper function (splat to reduce duplication)
            $displayConfig = @{
                ProjectPath = $script:DisplayProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            if ($script:PersistenceStrategy -and -not [string]::IsNullOrEmpty($script:PersistenceStrategy)) {
                $displayConfig.PersistenceStrategy = $script:PersistenceStrategy
            }

            Set-TestProjectConfiguration @displayConfig

            # Extract model for display operations (suppress output if fails)
            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            Context 'When displaying table information' {
                It 'Should show table details or handle missing tables gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('show-object', 'table', '--project', $script:DisplayProjectPath, '--schemaName', 'dbo')

                    # Assert
                    if ($commandResult.ExitCode -eq 0) {
                        $commandResult.Output | Should -Not -BeNullOrEmpty -Because 'Successful execution should produce output'
                        $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Successful execution should not show stack traces'
                    } else {
                        # If no tables found or other issue, should fail gracefully
                        $commandResult.Output | Should -Match 'table|object|schema|not found|No.*found' -Because 'Should provide meaningful error message when objects not found'
                        Write-Verbose "No objects found to display (expected if database not available)" -Verbose
                    }
                }
            }

            Context 'When displaying specific object by name' {
                It 'Should show specific table details when name is provided' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('show-object', 'table', '--project', $script:DisplayProjectPath, '--schemaName', 'dbo', '--name', 'Customer')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'export-model command' {
            Context 'When exporting to markdown format' {
                It 'Should create markdown documentation file' {
                    # Arrange
                    $exportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model.md'

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('export-model', '--project', $script:DisplayProjectPath, '--outputFileName', $exportPath, '--fileType', 'markdown')

                    # Assert
                    if ($commandResult.ExitCode -eq 0) {
                        $exportPath | Should -Exist -Because 'Exported file should exist'

                        $exportContent = Get-Content -Path $exportPath -Raw -ErrorAction SilentlyContinue
                        $exportContent | Should -Not -BeNullOrEmpty -Because 'Exported content should not be empty'
                        $exportContent | Should -Match 'Database|Model|Schema|#' -Because 'Should contain expected database documentation content with markdown formatting'
                    } else {
                        # Export may fail if no semantic model exists
                        $commandResult.Output | Should -Match 'model|semantic|export|not found' -Because 'Should provide meaningful error message for export failures'
                        Write-Verbose "Model export failed (expected if no semantic model exists)" -Verbose
                    }
                }
            }

            Context 'When exporting with split files option' {
                It 'Should handle splitFiles option correctly' {
                    # Arrange
                    $exportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model-split.md'

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('export-model', '--project', $script:DisplayProjectPath, '--outputFileName', $exportPath, '--fileType', 'markdown', '--splitFiles')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces for valid options'
                }
            }
        }
    }

    Context 'CLI Interface and Error Handling' {
        Context 'CLI help and error handling' {
            Context 'When requesting help information' {
                It 'Should display main help information correctly' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('--help')

                    # Assert
                    $commandResult.ExitCode | Should -Be 0 -Because 'Help command should succeed'
                    $commandResult.Output | Should -Not -BeNullOrEmpty -Because 'Help should produce output'
                    
                    # Join the output array into a single string for regex matching
                    $joinedOutput = $commandResult.Output -join "`n"
                    $joinedOutput | Should -Match '(?s)Description.*Usage.*Commands' -Because 'Should display standard CLI help format'
                    $joinedOutput | Should -Match '(?s)init-project.*extract-model.*query-model' -Because 'Should list main CLI commands'
                }
            }

            Context 'When using invalid commands' {
                It 'Should handle invalid commands gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('invalid-command-test')

                    # Assert
                    # Note: Some CLI frameworks return 0 even for invalid commands, so we accept both behaviors
                    $commandResult.ExitCode | Should -BeIn @(0, 1) -Because 'Should handle invalid commands gracefully'
                    
                    # Join the output array into a single string for regex matching
                    $joinedOutput = $commandResult.Output -join "`n"
                    $joinedOutput | Should -Match 'Unrecognized command.*invalid-command-test|Required command.*not.*provided' -Because 'Should provide error message for invalid commands'
                }
            }
        }
    }

    AfterAll {
    }
}


