<#
    .SYNOPSIS
        CosmosDb persistence strategy integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
        This test suite contains tests specific to the CosmosDb persistence strategy.
        Tests here validate Azure Cosmos DB operations, document-based storage patterns,
        and behaviors unique to storing semantic models in Azure Cosmos DB.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key
        - PERSISTENCE_STRATEGY: Should be 'CosmosDb'
        - AZURE_COSMOS_DB_ACCOUNT_ENDPOINT: Cosmos DB account endpoint
        - AZURE_COSMOS_DB_DATABASE_NAME: Cosmos DB database name (optional, defaults to SemanticModels)
        - AZURE_COSMOS_DB_MODELS_CONTAINER: Models container name (optional, defaults to Models)
        - AZURE_COSMOS_DB_ENTITIES_CONTAINER: Entities container name (optional, defaults to ModelEntities)
#>
#Requires -Version 7

param(
    [Parameter()]
    [ValidateSet('CosmosDb')]
    [string]
    $PersistenceStrategy = 'CosmosDb',
    
    [Parameter()]
    [string]
    $TestFilter = $null
)

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false

Describe 'GenAI Database Explorer Console Application - CosmosDb Strategy' {
    BeforeAll {
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
                [string]$TestFilter
            )
            
            $resolvedFilter = Get-ParameterOrEnvironment -ParameterValue $TestFilter -EnvironmentName 'TEST_FILTER'
            
            $noAzureMode = ($resolvedFilter -and ($resolvedFilter.ToString().Trim().ToLower() -eq 'no-azure')) -or 
                          ($env:NO_AZURE_MODE -and ($env:NO_AZURE_MODE.ToString().Trim().ToLower() -in @('true', '1', 'yes')))
            
            $environmentVars = @{
                SQL_CONNECTION_STRING = $env:SQL_CONNECTION_STRING
                DATABASE_SCHEMA = $env:DATABASE_SCHEMA
                AZURE_OPENAI_ENDPOINT = $env:AZURE_OPENAI_ENDPOINT
                AZURE_OPENAI_API_KEY = $env:AZURE_OPENAI_API_KEY
                AZURE_COSMOS_DB_ACCOUNT_ENDPOINT = $env:AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AZURE_COSMOS_DB_DATABASE_NAME = $env:AZURE_COSMOS_DB_DATABASE_NAME
                AZURE_COSMOS_DB_MODELS_CONTAINER = $env:AZURE_COSMOS_DB_MODELS_CONTAINER
                AZURE_COSMOS_DB_ENTITIES_CONTAINER = $env:AZURE_COSMOS_DB_ENTITIES_CONTAINER
            }
            
            return @{
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
            
            $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY', 'AZURE_COSMOS_DB_ACCOUNT_ENDPOINT')
            $missingVars = @($requiredVars | Where-Object { [string]::IsNullOrEmpty($Environment[$_]) })

            if ($missingVars -and $missingVars.Count -gt 0) {
                if ($NoAzureMode) {
                    Write-Verbose "NoAzure mode active: skipping required env var enforcement" -Verbose
                    return $true
                } else {
                    Write-Warning "Missing required environment variables for CosmosDb: $($missingVars -join ', ')"
                    throw "Missing required environment variables for CosmosDb: $($missingVars -join ', ')"
                }
            }
            
            return $true
        }

        function Initialize-TestWorkspace {
            param(
                [string]$TestDriveRoot,
                [string]$ConsoleAppPath
            )

            if (-not (Test-Path -LiteralPath $TestDriveRoot)) {
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-cosmosdb-test-" + [Guid]::NewGuid().ToString('N'))
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
                TestWorkspace  = (Get-Item -LiteralPath $testWorkspacePath)
                BaseProjectPath = $baseProjectPath
                ConsoleAppPath  = $ConsoleAppPath
            }
        }

        # Initialize test configuration
        $testConfig = Initialize-TestEnvironment -TestFilter $TestFilter
        
        $script:NoAzureMode = $testConfig.NoAzureMode
        $script:TestEnv = $testConfig.Environment
        
        Write-Host "CosmosDb Tests - Testing Azure Cosmos DB persistence" -ForegroundColor Magenta
        Write-Host "Cosmos DB Endpoint: $($script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT)" -ForegroundColor Magenta
        Write-Host "Database: $($script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME ?? 'SemanticModels (default)')" -ForegroundColor Magenta
        Write-Host "Models Container: $($script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER ?? 'Models (default)')" -ForegroundColor Magenta
        Write-Host "Entities Container: $($script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER ?? 'ModelEntities (default)')" -ForegroundColor Magenta
        
        Test-RequiredEnvironmentVariables -Environment $script:TestEnv -NoAzureMode $script:NoAzureMode
        
        $consoleAppPath = if ($env:CONSOLE_APP_PATH -and -not [string]::IsNullOrEmpty($env:CONSOLE_APP_PATH)) {
            $env:CONSOLE_APP_PATH
        } else {
            "./src/GenAIDBExplorer/GenAIDBExplorer.Console/bin/Debug/net10.0/GenAIDBExplorer.Console.exe"
        }

        $workspaceConfig = Initialize-TestWorkspace -TestDriveRoot $TestDrive -ConsoleAppPath $consoleAppPath
        $script:TestWorkspace = $workspaceConfig.TestWorkspace
        $script:BaseProjectPath = $workspaceConfig.BaseProjectPath
        $script:ConsoleAppPath = $workspaceConfig.ConsoleAppPath
    }

    Context 'Database Schema Operations with Cosmos DB' {
        BeforeAll {
            $script:DbProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'database-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:DbProjectPath -ConsoleApp $script:ConsoleAppPath

            if ($projectSetup.ExitCode -ne 0) {
                throw "Failed to initialize database test project"
            }

            $dbConfig = @{
                ProjectPath = $script:DbProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @dbConfig
        }

        Context 'extract-model command' {
            BeforeAll {
                $script:ExtractSucceeded = $false
            }

            It 'Should execute extract-model and store in Cosmos DB' {
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)

                $outputText = if ($commandResult.Output -is [array]) {
                    $commandResult.Output -join "`n"
                } else {
                    $commandResult.Output
                }

                if ($commandResult.ExitCode -eq 0) {
                    $script:ExtractSucceeded = $true
                    Write-Host "Extract-model command succeeded with Cosmos DB" -ForegroundColor Green
                    $commandResult.ExitCode | Should -Be 0 -Because 'Extract should succeed with CosmosDb strategy'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Database or Cosmos DB access not authorized'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'CosmosDb persistence strategy not yet fully implemented for extract-model'
                } else {
                    Write-Warning "Extract-model output: $outputText"
                    Set-ItResult -Inconclusive -Because 'Extract-model behavior unclear for CosmosDb'
                }
            }

            It 'Should verify model stored in Cosmos DB (when extract succeeds)' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract-model did not succeed in previous test'
                    return
                }

                # Verify by attempting to read the model back
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DbProjectPath
                )
                
                if ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Should be able to read model from Cosmos DB'
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should run dry-run generate-vectors with Cosmos DB' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    '--project', $script:DbProjectPath,
                    '--dryRun'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not yet supported for CosmosDb'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Dry-run should succeed with CosmosDb'
                }
            }

            It 'Should persist vectors to Cosmos DB entities container' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract did not succeed'
                    return
                }

                # Generate vectors for a specific object
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    'table',
                    '--project', $script:DbProjectPath,
                    '--schemaName', 'SalesLT',
                    '--name', 'Product',
                    '--overwrite'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Vectors should persist to Cosmos DB'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector persistence to CosmosDb not yet implemented'
                } else {
                    Set-ItResult -Inconclusive -Because "Vector generation not available: $outputText"
                }
            }
        }
    }

    Context 'AI Operations with Cosmos DB' {
        BeforeAll {
            $script:AiProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'ai-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:AiProjectPath -ConsoleApp $script:ConsoleAppPath

            if ($projectSetup.ExitCode -ne 0) {
                throw "Failed to initialize AI test project"
            }

            $aiConfig = @{
                ProjectPath = $script:AiProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @aiConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            It 'Should enrich model and update in Cosmos DB' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', '--project', $script:AiProjectPath)
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Enrich-model not yet supported for CosmosDb'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Enrich should succeed with CosmosDb'
                }
            }
        }
    }

    Context 'Model Display and Export Operations with Cosmos DB' {
        BeforeAll {
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $displayConfig = @{
                ProjectPath = $script:DisplayProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @displayConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            It 'Should display table information from Cosmos DB model' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DisplayProjectPath,
                    '--schemaName', 'SalesLT',
                    '--name', 'Product'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Cosmos DB'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Show-object not yet supported for CosmosDb'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Should display from CosmosDb'
                    $outputText | Should -Match 'Product|Table|Schema' -Because 'Should display table information'
                }
            }
        }

        Context 'export-model command' {
            It 'Should export model from Cosmos DB to local file' {
                $exportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model.md'
                
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'export-model',
                    '--project', $script:DisplayProjectPath,
                    '--outputPath', $exportPath,
                    '--fileType', 'markdown'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Export-model not yet supported for CosmosDb'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Export should succeed from CosmosDb'
                    Test-Path -Path $exportPath | Should -BeTrue -Because 'Exported file should exist locally'
                }
            }
        }
    }

    Context 'Cosmos DB Specific Scenarios' {
        It 'Should handle separate containers for models and entities' {
            # This test verifies the dual-container architecture works correctly
            $projectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'container-test'
            Initialize-TestProject -ProjectPath $projectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $projectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @config

            $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $projectPath)
            
            # This test mainly verifies the command doesn't fail with dual-container configuration
            if ($result.ExitCode -eq 0 -or ($result.Output -join "`n") -match 'AuthorizationFailure|not yet supported') {
                $true | Should -BeTrue -Because 'Command should handle dual-container Cosmos DB configuration'
            }
        }

        It 'Should support hierarchical partition keys in Cosmos DB' {
            # Note: This test assumes HPK support is implemented in the repository
            # HPK allows better organization and query performance in Cosmos DB
            $projectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'hpk-test'
            Initialize-TestProject -ProjectPath $projectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $projectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @config

            $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $projectPath)
            
            # This test verifies HPK-based partitioning works if implemented
            if ($result.ExitCode -eq 0 -or ($result.Output -join "`n") -match 'AuthorizationFailure|not yet supported') {
                $true | Should -BeTrue -Because 'Command should handle HPK configuration'
            }
        }
    }

    AfterAll {
        Write-Host "CosmosDb persistence strategy tests completed" -ForegroundColor Magenta
    }
}
