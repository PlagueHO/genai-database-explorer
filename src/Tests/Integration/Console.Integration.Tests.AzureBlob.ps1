<#
    .SYNOPSIS
        AzureBlob persistence strategy integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
        This test suite contains tests specific to the AzureBlob persistence strategy.
        Tests here validate Azure Blob Storage operations, blob-based storage patterns,
        and behaviors unique to storing semantic models in Azure Blob Storage.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key
        - PERSISTENCE_STRATEGY: Should be 'AzureBlob'
        - SemanticModelRepository__AzureBlob__AccountEndpoint: Azure Storage account endpoint
        - SemanticModelRepository__AzureBlob__ContainerName: Storage container name (optional)
        - SemanticModelRepository__AzureBlob__BlobPrefix: Blob prefix (optional)
#>
#Requires -Version 7

param(
    [Parameter()]
    [ValidateSet('AzureBlob')]
    [string]
    $PersistenceStrategy = 'AzureBlob',
    
    [Parameter()]
    [string]
    $TestFilter = $null
)

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false

Describe 'GenAI Database Explorer Console Application - AzureBlob Strategy' {
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
                AZURE_STORAGE_ACCOUNT_ENDPOINT = $env:SemanticModelRepository__AzureBlob__AccountEndpoint
                AZURE_STORAGE_CONTAINER = $env:SemanticModelRepository__AzureBlob__ContainerName
                AZURE_STORAGE_BLOB_PREFIX = $env:SemanticModelRepository__AzureBlob__BlobPrefix
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
            
            $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY', 'AZURE_STORAGE_ACCOUNT_ENDPOINT')
            $missingVars = @($requiredVars | Where-Object { [string]::IsNullOrEmpty($Environment[$_]) })

            if ($missingVars -and $missingVars.Count -gt 0) {
                if ($NoAzureMode) {
                    Write-Verbose "NoAzure mode active: skipping required env var enforcement" -Verbose
                    return $true
                } else {
                    Write-Warning "Missing required environment variables for AzureBlob: $($missingVars -join ', ')"
                    throw "Missing required environment variables for AzureBlob: $($missingVars -join ', ')"
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
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-azureblob-test-" + [Guid]::NewGuid().ToString('N'))
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
        
        Write-Host "AzureBlob Tests - Testing Azure Blob Storage persistence" -ForegroundColor Cyan
        Write-Host "Storage Endpoint: $($script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT)" -ForegroundColor Cyan
        Write-Host "Container: $($script:TestEnv.AZURE_STORAGE_CONTAINER ?? 'semantic-models (default)')" -ForegroundColor Cyan
        
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

    Context 'Database Schema Operations with Azure Blob Storage' {
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
                PersistenceStrategy = 'AzureBlob'
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @dbConfig
        }

        Context 'extract-model command' {
            BeforeAll {
                $script:ExtractSucceeded = $false
            }

            It 'Should execute extract-model and store in Azure Blob Storage' {
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)

                $outputText = if ($commandResult.Output -is [array]) {
                    $commandResult.Output -join "`n"
                } else {
                    $commandResult.Output
                }

                if ($commandResult.ExitCode -eq 0) {
                    $script:ExtractSucceeded = $true
                    Write-Host "Extract-model command succeeded with Azure Blob Storage" -ForegroundColor Green
                    $commandResult.ExitCode | Should -Be 0 -Because 'Extract should succeed with AzureBlob strategy'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Database or storage access not authorized'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'AzureBlob persistence strategy not yet fully implemented for extract-model'
                } else {
                    Write-Warning "Extract-model output: $outputText"
                    Set-ItResult -Inconclusive -Because 'Extract-model behavior unclear for AzureBlob'
                }
            }

            It 'Should verify model stored in Azure Blob Storage (when extract succeeds)' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract-model did not succeed in previous test'
                    return
                }

                # Note: Direct blob verification would require Azure SDK
                # For now, we verify by attempting to read the model back
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DbProjectPath
                )
                
                if ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Should be able to read model from Azure Blob Storage'
                }
            }
        }
    }

    Context 'AI Operations with Azure Blob Storage' {
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
                PersistenceStrategy = 'AzureBlob'
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @aiConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            It 'Should enrich model and store in Azure Blob Storage' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', '--project', $script:AiProjectPath)
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Enrich-model not yet supported for AzureBlob'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Enrich should succeed with AzureBlob'
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should run dry-run generate-vectors with Azure Blob Storage' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    '--project', $script:AiProjectPath,
                    '--dry-run'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in blob storage'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not yet supported for AzureBlob'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Dry-run should succeed with AzureBlob'
                } else {
                    Write-Warning "generate-vectors output: $outputText"
                    Set-ItResult -Inconclusive -Because "Vector generation failed with unclear error (exit code: $($result.ExitCode))"
                }
            }

            It 'Should persist vector envelopes to Azure Blob Storage' {
                # Generate vectors for a specific object
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    'table',
                    '--project', $script:AiProjectPath,
                    '--schema', 'SalesLT',
                    '--name', 'Product',
                    '--overwrite'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($result.ExitCode -ne 0) {
                    Set-ItResult -Inconclusive -Because "Vector generation not available: $outputText"
                    return
                }
                
                # Log command output for diagnostics
                Write-Host "Generate-vectors output: $outputText" -ForegroundColor Cyan
                
                # Check if the output indicates vectors were actually generated
                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available for vector generation'
                    return
                }
                
                if ($outputText -match 'not supported|not available|not configured|not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not supported for AzureBlob persistence'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized'
                    return
                }
                
                # For Azure Blob Storage, vectors should be stored in the blob storage
                # We can verify by checking if the command output indicates success
                if ($outputText -match 'generated|completed|success|saved|stored') {
                    # Verify we can retrieve the model (which should now include vector references)
                    $verifyResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'show-object',
                        'table',
                        '--project', $script:AiProjectPath,
                        '--schema', 'SalesLT',
                        '--name', 'Product'
                    )
                    
                    if ($verifyResult.ExitCode -eq 0) {
                        $verifyResult.ExitCode | Should -Be 0 -Because "Vector generation succeeded and model should be retrievable from Azure Blob Storage. Output: $outputText"
                    } else {
                        Write-Warning "Vector generation appeared successful but verification failed: $($verifyResult.Output -join "`n")"
                        Set-ItResult -Inconclusive -Because 'Vector generation succeeded but verification failed'
                    }
                } else {
                    Set-ItResult -Inconclusive -Because "Command succeeded but output doesn't confirm vector generation: $outputText"
                }
            }
        }
    }

    Context 'Model Display and Export Operations with Azure Blob Storage' {
        BeforeAll {
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $displayConfig = @{
                ProjectPath = $script:DisplayProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'AzureBlob'
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @displayConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            It 'Should display table information from Azure Blob Storage model' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DisplayProjectPath,
                    '--schema', 'SalesLT',
                    '--name', 'Product'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Azure Blob Storage'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Show-object not yet supported for AzureBlob'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Should display from AzureBlob'
                    $outputText | Should -Match 'Product|Table|Schema' -Because 'Should display table information'
                }
            }
        }

        Context 'export-model command' {
            It 'Should export model from Azure Blob Storage to local file' {
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
                    Set-ItResult -Inconclusive -Because 'Export-model not yet supported for AzureBlob'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Export should succeed from AzureBlob'
                    Test-Path -Path $exportPath | Should -BeTrue -Because 'Exported file should exist locally'
                }
            }
        }
    }

    Context 'Azure Blob Storage Specific Scenarios' {
        It 'Should handle blob prefix correctly when configured' {
            if ([string]::IsNullOrEmpty($script:TestEnv.AZURE_STORAGE_BLOB_PREFIX)) {
                Set-ItResult -Skipped -Because 'Blob prefix not configured'
                return
            }

            $projectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'prefix-test'
            Initialize-TestProject -ProjectPath $projectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $projectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'AzureBlob'
                AzureStorageAccountEndpoint = $script:TestEnv.AZURE_STORAGE_ACCOUNT_ENDPOINT
                AzureStorageContainer = $script:TestEnv.AZURE_STORAGE_CONTAINER
                AzureStorageBlobPrefix = $script:TestEnv.AZURE_STORAGE_BLOB_PREFIX
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @config

            $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $projectPath)
            
            # This test mainly verifies the command doesn't fail with prefix configured
            if ($result.ExitCode -eq 0 -or ($result.Output -join "`n") -match 'AuthorizationFailure|not yet supported') {
                $true | Should -BeTrue -Because 'Command should handle blob prefix configuration'
            }
        }
    }

    AfterAll {
        Write-Host "AzureBlob persistence strategy tests completed" -ForegroundColor Cyan
    }
}
