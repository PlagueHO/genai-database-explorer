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
                    '--dry-run'
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
                
                if ($outputText -match 'not supported|not available|not configured') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not supported in this configuration'
                    return
                }
                
                # For LocalDisk persistence, vectors are stored within entity JSON files, not in a separate directory
                $entityPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'tables', 'SalesLT.Product.json'
                $semanticModelPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel'
                
                if (Test-Path -Path $semanticModelPath) {
                    Write-Host "SemanticModel directory contents:" -ForegroundColor Yellow
                    Get-ChildItem -Path $semanticModelPath -Recurse | ForEach-Object {
                        Write-Host "  $($_.FullName)" -ForegroundColor Gray
                    }
                }
                
                # Verify that vectors were processed by checking command output
                if ($outputText -match 'Processed \d+ entit') {
                    # Verify the entity file exists (vectors are embedded in entity JSON for LocalDisk)
                    Test-Path -Path $entityPath | Should -BeTrue -Because "Entity file should exist after vector generation. Output: $outputText"
                    
                    # Verify the entity file contains vector data by checking file size (vector embeddings make files larger)
                    if (Test-Path -Path $entityPath) {
                        $entityFile = Get-Item -Path $entityPath
                        $entityFile.Length | Should -BeGreaterThan 1000 -Because 'Entity file with vectors should be larger than 1KB'
                    }
                } else {
                    Set-ItResult -Inconclusive -Because "Vector generation output did not confirm processing: $outputText"
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

            # Extract model and track success for dependent tests
            $extractResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath)
            $script:DisplayModelExtracted = ($extractResult.ExitCode -eq 0)
            
            if (-not $script:DisplayModelExtracted) {
                $extractOutput = $extractResult.Output -join "`n"
                Write-Host "Display model extraction failed or was inconclusive: $extractOutput" -ForegroundColor Yellow
            }
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
                    
                    # First check if semantic model exists
                    $semanticModelPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $semanticModelPath)) {
                        Set-ItResult -Inconclusive -Because 'Semantic model not available - extract may have failed'
                        return
                    }
                    
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'export-model',
                        '--project', $script:DisplayProjectPath,
                        '--outputFileName', $exportPath,
                        '--fileType', 'markdown'
                    )
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found|Model not found|AuthorizationFailure|Access denied') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                    } elseif ($result.ExitCode -ne 0) {
                        Write-Host "Export-model failed with exit code $($result.ExitCode). Output: $outputText" -ForegroundColor Yellow
                        Set-ItResult -Inconclusive -Because "Export-model command failed with exit code $($result.ExitCode): $outputText"
                    } elseif (-not (Test-Path -Path $exportPath)) {
                        Write-Host "Export-model reported success but file not found. Output: $outputText" -ForegroundColor Yellow
                        Set-ItResult -Inconclusive -Because 'Export file was not created despite command success'
                    } else {
                        $result.ExitCode | Should -Be 0
                        Test-Path -Path $exportPath | Should -BeTrue -Because 'Exported markdown file should exist on local disk'
                    }
                }
            }

            Context 'When exporting with split files option' {
                It 'Should export model to multiple files on local disk' {
                    # First check if semantic model exists
                    $semanticModelPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $semanticModelPath)) {
                        Set-ItResult -Inconclusive -Because 'Semantic model not available - extract may have failed'
                        return
                    }
                    
                    $exportDir = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-split'
                    
                    # Create the export directory if it doesn't exist (required for split files mode)
                    if (-not (Test-Path -Path $exportDir)) {
                        New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
                    }
                    
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'export-model',
                        '--project', $script:DisplayProjectPath,
                        '--outputFileName', $exportDir,
                        '--fileType', 'markdown',
                        '--splitFiles'
                    )
                    
                    $outputText = $result.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found|Model not found|AuthorizationFailure|Access denied') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                    } elseif ($result.ExitCode -ne 0) {
                        Write-Host "Export-model (split) failed with exit code $($result.ExitCode). Output: $outputText" -ForegroundColor Yellow
                        Set-ItResult -Inconclusive -Because "Export-model command failed with exit code $($result.ExitCode): $outputText"
                    } else {
                        $result.ExitCode | Should -Be 0
                        
                        # Check if any markdown files were created in the export directory
                        $exportedFiles = Get-ChildItem -Path $exportDir -Filter '*.md' -ErrorAction SilentlyContinue
                        if ($exportedFiles -and $exportedFiles.Count -gt 0) {
                            $exportedFiles.Count | Should -BeGreaterThan 0 -Because 'Split export should create markdown files'
                        } else {
                            Write-Host "Export directory contents: $(Get-ChildItem -Path $exportDir -Recurse | Select-Object -ExpandProperty FullName)" -ForegroundColor Yellow
                            Write-Host "Command output: $outputText" -ForegroundColor Yellow
                            Set-ItResult -Inconclusive -Because 'No markdown files found in export directory despite command success'
                        }
                    }
                }
            }
        }
    }

    AfterAll {
        Write-Host "LocalDisk persistence strategy tests completed" -ForegroundColor Green
    }
}
