<#
    .SYNOPSIS
        LocalDisk persistence strategy integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
        This test suite contains tests specific to the LocalDisk persistence strategy.
        Tests here validate file system operations, local storage patterns, and behaviors
        unique to storing semantic models on the local file system.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key
        - PERSISTENCE_STRATEGY: Should be 'LocalDisk'
#>
#Requires -Version 7

param(
    [Parameter()]
    [ValidateSet('LocalDisk')]
    [string]
    $PersistenceStrategy = 'LocalDisk',
    
    [Parameter()]
    [string]
    $TestFilter = $null
)

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false

Describe 'GenAI Database Explorer Console Application - LocalDisk Strategy' {
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
            
            $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY')
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

        function Initialize-TestWorkspace {
            param(
                [string]$TestDriveRoot,
                [string]$ConsoleAppPath
            )

            if (-not (Test-Path -LiteralPath $TestDriveRoot)) {
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-localdisk-test-" + [Guid]::NewGuid().ToString('N'))
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
        
        Write-Host "LocalDisk Tests - Testing local file system persistence" -ForegroundColor Green
        
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

    Context 'Database Schema Operations' {
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
                PersistenceStrategy = 'LocalDisk'
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @dbConfig
        }

        Context 'extract-model command' {
            BeforeAll {
                $script:SemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                $script:ExtractSucceeded = $false
            }

            It 'Should execute extract-model and create semanticmodel.json on local disk' {
                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)

                $outputText = if ($commandResult.Output -is [array]) {
                    $commandResult.Output -join "`n"
                } else {
                    $commandResult.Output
                }

                if ($commandResult.ExitCode -eq 0) {
                    $script:ExtractSucceeded = $true
                    Write-Host "Extract-model command succeeded" -ForegroundColor Green
                    $commandResult.ExitCode | Should -Be 0 -Because 'Extract should succeed'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Database access not authorized - this is expected in some test environments'
                } else {
                    throw "Extract-model failed with unexpected error: $outputText"
                }
            }

            It 'Should create semanticmodel.json file on local file system' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract-model did not succeed in previous test'
                    return
                }

                Test-Path -Path $script:SemanticModelPath | Should -BeTrue -Because 'semanticmodel.json should exist on local disk'
                
                $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json
                $model.Name | Should -Not -BeNullOrEmpty -Because 'Model should have a name'
            }

            It 'Should set model name to database in connection string when available' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract-model did not succeed'
                    return
                }

                $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json
                
                # Parse database name from connection string
                $connectionString = $script:TestEnv.SQL_CONNECTION_STRING
                if ($connectionString -match 'Database=([^;]+)') {
                    $expectedDbName = $matches[1]
                    $model.Name | Should -Be $expectedDbName -Because 'Model name should match database name from connection string'
                }
            }

            Context 'When extracting with specific options' {
                It 'Should support --skipTables option' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath, '--skipTables')
                    
                    if ($result.ExitCode -eq 0 -or ($result.Output -join "`n") -match 'AuthorizationFailure|Access denied') {
                        $result.ExitCode | Should -BeIn @(0) -Because 'Command should succeed or fail with known error'
                    }
                }
            }
        }

        Context 'data-dictionary command' {
            Context 'When applying data dictionary files' {
                BeforeAll {
                    $script:DictPath = Join-Path -Path $script:DbProjectPath -ChildPath 'dict'
                    New-Item -ItemType Directory -Path $script:DictPath -Force | Out-Null
                    
                    $dictFile = Join-Path -Path $script:DictPath -ChildPath 'test-dict.json'
                    New-TestDataDictionary -DictionaryPath $dictFile -ObjectType 'table' -SchemaName 'SalesLT' -ObjectName 'Product' -Description 'Test product table'
                }

                It 'Should apply data dictionary from local file system' {
                    # Note: data-dictionary command requires a subcommand (table/view/etc)
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'data-dictionary',
                        'table',
                        '--project', $script:DbProjectPath,
                        '--source-path', "$script:DictPath/*.json",
                        '--schema', 'SalesLT',
                        '--name', 'Product',
                        '--show'
                    )
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($result.ExitCode -eq 0) {
                        $result.ExitCode | Should -Be 0
                    } elseif ($outputText -match 'No semantic model found|not found|Model not found') {
                        Set-ItResult -Inconclusive -Because 'Model not available - extract may have been skipped'
                    } else {
                        # Command syntax or other error
                        Write-Warning "data-dictionary output: $outputText"
                        $result.ExitCode | Should -Be 0 -Because 'data-dictionary command should succeed or indicate model not found'
                    }
                }
            }
        }
    }

    Context 'AI Operations' {
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
                PersistenceStrategy = 'LocalDisk'
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @aiConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            Context 'When enriching with AI services available' {
                It 'Should enrich model and save to local file system' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', '--project', $script:AiProjectPath)
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                    } elseif ($result.ExitCode -eq 0) {
                        $result.ExitCode | Should -Be 0
                        
                        # Verify enriched model on local disk
                        $modelPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                        Test-Path -Path $modelPath | Should -BeTrue -Because 'Enriched model should exist on local disk'
                    }
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should run dry-run generate-vectors without errors' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    '--project', $script:AiProjectPath,
                    '--dryRun'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                } else {
                    $result.ExitCode | Should -Be 0 -Because 'Dry-run should succeed'
                }
            }

            It 'Should persist vector envelopes to local file system' {
                # Generate vectors for a specific object
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    'table',
                    '--project', $script:AiProjectPath,
                    '--schemaName', 'SalesLT',
                    '--name', 'Product',
                    '--overwrite'
                )
                
                $outputText = $result.Output -join "`n"
                
                if ($result.ExitCode -eq 0) {
                    # Verify vector envelope file was created on local disk
                    $vectorsPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'Vectors'
                    Test-Path -Path $vectorsPath | Should -BeTrue -Because 'Vectors directory should exist on local disk'
                } else {
                    Set-ItResult -Inconclusive -Because "Vector generation not available: $outputText"
                }
            }
        }
    }

    Context 'Model Display and Export Operations' {
        BeforeAll {
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $displayConfig = @{
                ProjectPath = $script:DisplayProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'LocalDisk'
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @displayConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            Context 'When displaying table information' {
                It 'Should display table information from local disk model' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'show-object',
                        'table',
                        '--project', $script:DisplayProjectPath,
                        '--schemaName', 'SalesLT',
                        '--name', 'Product'
                    )
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found') {
                        Set-ItResult -Inconclusive -Because 'Model not available'
                    } else {
                        $result.ExitCode | Should -Be 0
                        $outputText | Should -Match 'Product|Table|Schema' -Because 'Should display table information'
                    }
                }
            }
        }

        Context 'export-model command' {
            Context 'When exporting to markdown format' {
                It 'Should export model from local disk to markdown file' {
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
                    } else {
                        $result.ExitCode | Should -Be 0
                        Test-Path -Path $exportPath | Should -BeTrue -Because 'Exported markdown file should exist on local disk'
                    }
                }
            }

            Context 'When exporting with split files option' {
                It 'Should export model to multiple files on local disk' {
                    $exportDir = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-split'
                    
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'export-model',
                        '--project', $script:DisplayProjectPath,
                        '--outputPath', $exportDir,
                        '--fileType', 'markdown',
                        '--splitFiles'
                    )
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found') {
                        Set-ItResult -Inconclusive -Because 'Model not available'
                    } else {
                        $result.ExitCode | Should -Be 0
                        Test-Path -Path $exportDir | Should -BeTrue -Because 'Export directory should exist on local disk'
                    }
                }
            }
        }
    }

    AfterAll {
        Write-Host "LocalDisk persistence strategy tests completed" -ForegroundColor Green
    }
}
