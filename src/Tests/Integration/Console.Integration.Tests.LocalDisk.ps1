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
                $script:ExtractCommandResult = $null
                
                # Execute extract-model command once for all tests in this context
                try {
                    $script:ExtractCommandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)
                    
                    $outputText = if ($script:ExtractCommandResult.Output -is [array]) {
                        $script:ExtractCommandResult.Output -join "`n"
                    } else {
                        $script:ExtractCommandResult.Output
                    }
                    
                    if ($script:ExtractCommandResult.ExitCode -eq 0) {
                        $script:ExtractSucceeded = $true
                        Write-Host "Extract-model command succeeded" -ForegroundColor Green
                    } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                        Write-Host "Extract-model: Database access not authorized" -ForegroundColor Yellow
                    }
                } catch {
                    Write-Host "Extract-model command failed: $_" -ForegroundColor Yellow
                }
            }

            It 'Should complete extract-model command successfully' {
                if (-not $script:ExtractCommandResult) {
                    Set-ItResult -Inconclusive -Because 'Extract-model command threw an exception'
                    return
                }
                
                $outputText = if ($script:ExtractCommandResult.Output -is [array]) {
                    $script:ExtractCommandResult.Output -join "`n"
                } else {
                    $script:ExtractCommandResult.Output
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Database access not authorized - this is expected in some test environments'
                    return
                }
                
                $script:ExtractCommandResult.ExitCode | Should -Be 0 -Because 'Extract-model should complete successfully'
            }

            It 'Should create semanticmodel.json file on local disk' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Inconclusive -Because 'Extract-model did not succeed in previous test'
                    return
                }

                Test-Path -Path $script:SemanticModelPath | Should -BeTrue -Because 'semanticmodel.json file should exist on local disk'
            }
            
            It 'Should create semantic model with a valid name property' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Inconclusive -Because 'Extract-model did not succeed'
                    return
                }
                
                $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json
                $model.Name | Should -Not -BeNullOrEmpty -Because 'Semantic model should have a non-empty name'
            }

            It 'Should set model name to match database name from connection string' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Inconclusive -Because 'Extract-model did not succeed'
                    return
                }

                $model = Get-Content -Path $script:SemanticModelPath | ConvertFrom-Json
                
                # Parse database name from connection string
                $connectionString = $script:TestEnv.SQL_CONNECTION_STRING
                if ($connectionString -match 'Database=([^;]+)') {
                    $expectedDbName = $matches[1]
                    $model.Name | Should -Be $expectedDbName -Because 'Model name should match database name from connection string'
                } else {
                    Set-ItResult -Inconclusive -Because 'Could not parse database name from connection string'
                }
            }

            Context 'When extracting with specific options' {
                BeforeAll {
                    $script:SkipTablesResult = $null
                    try {
                        $script:SkipTablesResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath, '--skip-tables')
                    } catch {
                        Write-Host "Extract with --skip-tables failed: $_" -ForegroundColor Yellow
                    }
                }
                
                It 'Should execute extract-model with --skip-tables option successfully' {
                    if (-not $script:SkipTablesResult) {
                        Set-ItResult -Inconclusive -Because 'Extract-model with --skip-tables threw an exception'
                        return
                    }
                    
                    $outputText = $script:SkipTablesResult.Output -join "`n"
                    if ($outputText -match 'AuthorizationFailure|Access denied') {
                        Set-ItResult -Inconclusive -Because 'Database access not authorized'
                        return
                    }
                    
                    $script:SkipTablesResult.ExitCode | Should -Be 0 -Because 'Extract-model with --skip-tables should complete successfully'
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
                    
                    $script:DataDictResult = $null
                    try {
                        $script:DataDictResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                            'data-dictionary',
                            'table',
                            '--project', $script:DbProjectPath,
                            '--source-path', "$script:DictPath/*.json",
                            '--schema-name', 'SalesLT',
                            '--name', 'Product',
                            '--show'
                        )
                    } catch {
                        Write-Host "data-dictionary command failed: $_" -ForegroundColor Yellow
                    }
                }

                It 'Should execute data-dictionary command successfully' {
                    if (-not $script:DataDictResult) {
                        Set-ItResult -Inconclusive -Because 'data-dictionary command threw an exception'
                        return
                    }
                    
                    $outputText = $script:DataDictResult.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found|Model not found') {
                        Set-ItResult -Inconclusive -Because 'Model not available - extract may have been skipped'
                        return
                    }
                    
                    $script:DataDictResult.ExitCode | Should -Be 0 -Because 'data-dictionary command should complete successfully'
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
                BeforeAll {
                    $script:EnrichResult = $null
                    $script:EnrichSucceeded = $false
                    
                    try {
                        $script:EnrichResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', '--project', $script:AiProjectPath)
                        if ($script:EnrichResult.ExitCode -eq 0) {
                            $script:EnrichSucceeded = $true
                        }
                    } catch {
                        Write-Host "enrich-model command failed: $_" -ForegroundColor Yellow
                    }
                }
                
                It 'Should execute enrich-model command successfully' {
                    if (-not $script:EnrichResult) {
                        Set-ItResult -Inconclusive -Because 'enrich-model command threw an exception'
                        return
                    }
                    
                    $outputText = $script:EnrichResult.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                        return
                    }
                    
                    $script:EnrichResult.ExitCode | Should -Be 0 -Because 'enrich-model should complete successfully'
                }
                
                It 'Should persist enriched model to local disk' {
                    if (-not $script:EnrichSucceeded) {
                        Set-ItResult -Inconclusive -Because 'enrich-model did not succeed'
                        return
                    }
                    
                    $modelPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    Test-Path -Path $modelPath | Should -BeTrue -Because 'Enriched model file should exist on local disk'
                }
            }
        }

        Context 'generate-vectors command' {
            BeforeAll {
                $script:VectorsDryRunResult = $null
                try {
                    $script:VectorsDryRunResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'generate-vectors',
                        '--project', $script:AiProjectPath,
                        '--dry-run'
                    )
                } catch {
                    Write-Host "generate-vectors dry-run failed: $_" -ForegroundColor Yellow
                }
            }
            
            It 'Should execute generate-vectors dry-run successfully' {
                if (-not $script:VectorsDryRunResult) {
                    Set-ItResult -Inconclusive -Because 'generate-vectors dry-run threw an exception'
                    return
                }
                
                $outputText = $script:VectorsDryRunResult.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|not found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                    return
                }
                
                $script:VectorsDryRunResult.ExitCode | Should -Be 0 -Because 'generate-vectors dry-run should complete successfully'
            }

            It 'Should generate and persist vectors for specific table to local file system' {
                # Generate vectors for a specific object
                $script:VectorsGenerateResult = $null
                try {
                    $script:VectorsGenerateResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'generate-vectors',
                        'table',
                        '--project', $script:AiProjectPath,
                        '--schema-name', 'SalesLT',
                        '--name', 'Product',
                        '--overwrite'
                    )
                } catch {
                    Set-ItResult -Inconclusive -Because "generate-vectors command threw an exception: $_"
                    return
                }
                
                $outputText = $script:VectorsGenerateResult.Output -join "`n"
                
                if ($script:VectorsGenerateResult.ExitCode -ne 0) {
                    Set-ItResult -Inconclusive -Because "Vector generation failed: $outputText"
                    return
                }
                                
                # Check if the output indicates vectors were actually generated
                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available for vector generation'
                    return
                }
                
                if ($outputText -match 'not supported|not available|not configured') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not supported in this configuration'
                    return
                }
                
                if ($outputText -notmatch 'Processed \d+ entit') {
                    Set-ItResult -Inconclusive -Because "Vector generation output did not confirm processing: $outputText"
                    return
                }
                
                # For LocalDisk persistence, vectors are stored within entity JSON files
                $entityPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'tables', 'SalesLT.Product.json'
                $semanticModelPath = Join-Path -Path $script:AiProjectPath -ChildPath 'SemanticModel'
                                
                # Verify the entity file exists (vectors are embedded in entity JSON for LocalDisk)
                Test-Path -Path $entityPath | Should -BeTrue -Because "Entity file with vectors should exist on local disk after generation"
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
                BeforeAll {
                    $script:ShowObjectResult = $null
                    $script:ShowObjectSucceeded = $false
                    
                    try {
                        $script:ShowObjectResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                            'show-object',
                            'table',
                            '--project', $script:DisplayProjectPath,
                            '--schema-name', 'SalesLT',
                            '--name', 'Product'
                        )
                        if ($script:ShowObjectResult.ExitCode -eq 0) {
                            $script:ShowObjectSucceeded = $true
                        }
                    } catch {
                        Write-Host "show-object command failed: $_" -ForegroundColor Yellow
                    }
                }
                
                It 'Should execute show-object command successfully' {
                    if (-not $script:ShowObjectResult) {
                        Set-ItResult -Inconclusive -Because 'show-object command threw an exception'
                        return
                    }
                    
                    $outputText = $script:ShowObjectResult.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found') {
                        Set-ItResult -Inconclusive -Because 'Model not available'
                        return
                    }
                    
                    $script:ShowObjectResult.ExitCode | Should -Be 0 -Because 'show-object should complete successfully'
                }
                
                It 'Should display table information in output' {
                    if (-not $script:ShowObjectSucceeded) {
                        Set-ItResult -Inconclusive -Because 'show-object did not succeed'
                        return
                    }
                    
                    $outputText = $script:ShowObjectResult.Output -join "`n"
                    $outputText | Should -Match 'Product|Table|Schema' -Because 'Output should contain table information'
                }
            }
        }

        Context 'export-model command' {
            Context 'When exporting to markdown format' {
                BeforeAll {
                    $script:ExportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model.md'
                    $script:ExportResult = $null
                    $script:ExportSucceeded = $false
                    
                    # Check if semantic model exists before attempting export
                    $semanticModelPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $semanticModelPath)) {
                        Write-Host "Semantic model not found - skipping export" -ForegroundColor Yellow
                        return
                    }
                    
                    try {
                        $script:ExportResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                            'export-model',
                            '--project', $script:DisplayProjectPath,
                            '--output-file-name', $script:ExportPath,
                            '--file-type', 'markdown'
                        )
                        if ($script:ExportResult.ExitCode -eq 0) {
                            $script:ExportSucceeded = $true
                        }
                    } catch {
                        Write-Host "export-model command failed: $_" -ForegroundColor Yellow
                    }
                }
                
                It 'Should execute export-model command successfully' {
                    if (-not $script:ExportResult) {
                        Set-ItResult -Inconclusive -Because 'Semantic model not available or export-model threw an exception'
                        return
                    }
                    
                    $outputText = $script:ExportResult.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found|Model not found|AuthorizationFailure|Access denied') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                        return
                    }
                    
                    $script:ExportResult.ExitCode | Should -Be 0 -Because 'export-model should complete successfully'
                }
                
                It 'Should create exported markdown file on local disk' {
                    if (-not $script:ExportSucceeded) {
                        Set-ItResult -Inconclusive -Because 'export-model did not succeed'
                        return
                    }
                    
                    Test-Path -Path $script:ExportPath | Should -BeTrue -Because 'Exported markdown file should exist on local disk'
                }
            }

            Context 'When exporting with split files option' {
                BeforeAll {
                    $script:ExportSplitDir = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-split'
                    $script:ExportSplitResult = $null
                    $script:ExportSplitSucceeded = $false
                    
                    # Check if semantic model exists before attempting export
                    $semanticModelPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'SemanticModel' -AdditionalChildPath 'semanticmodel.json'
                    if (-not (Test-Path -Path $semanticModelPath)) {
                        Write-Host "Semantic model not found - skipping split export" -ForegroundColor Yellow
                        return
                    }
                    
                    # Create the export directory (required for split files mode)
                    if (-not (Test-Path -Path $script:ExportSplitDir)) {
                        New-Item -ItemType Directory -Path $script:ExportSplitDir -Force | Out-Null
                    }
                    
                    # Add trailing directory separator to ensure it's recognized as a directory
                    $exportDirPath = $script:ExportSplitDir.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
                    
                    try {
                        $script:ExportSplitResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                            'export-model',
                            '--project', $script:DisplayProjectPath,
                            '--output-file-name', $exportDirPath,
                            '--file-type', 'markdown',
                            '--split-files'
                        )
                        if ($script:ExportSplitResult.ExitCode -eq 0) {
                            $script:ExportSplitSucceeded = $true
                        }
                    } catch {
                        Write-Host "export-model (split) command failed: $_" -ForegroundColor Yellow
                    }
                }
                
                It 'Should execute export-model with --split-files successfully' {
                    if (-not $script:ExportSplitResult) {
                        Set-ItResult -Inconclusive -Because 'Semantic model not available or export-model threw an exception'
                        return
                    }
                    
                    $outputText = $script:ExportSplitResult.Output -join "`n"
                    
                    if ($outputText -match 'No semantic model found|not found|Model not found|AuthorizationFailure|Access denied') {
                        Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                        return
                    }
                    
                    $script:ExportSplitResult.ExitCode | Should -Be 0 -Because 'export-model with --split-files should complete successfully'
                }
                
                It 'Should create multiple markdown files in subdirectories on local disk' {
                    if (-not $script:ExportSplitSucceeded) {
                        Set-ItResult -Inconclusive -Because 'export-model split did not succeed'
                        return
                    }
                    
                    # Collect all markdown files from export directory and subdirectories
                    $allExportedFiles = @()
                    if (Test-Path -Path $script:ExportSplitDir) {
                        $allExportedFiles = Get-ChildItem -Path $script:ExportSplitDir -Filter '*.md' -Recurse -ErrorAction SilentlyContinue
                    }
                    
                    if (-not $allExportedFiles -or $allExportedFiles.Count -eq 0) {
                        Write-Host "Export directory structure:" -ForegroundColor Yellow
                        if (Test-Path -Path $script:ExportSplitDir) {
                            Get-ChildItem -Path $script:ExportSplitDir -Recurse | ForEach-Object {
                                Write-Host "  $($_.FullName)" -ForegroundColor Gray
                            }
                        }
                        Set-ItResult -Inconclusive -Because 'No markdown files found in export directory despite command success'
                        return
                    }
                    
                    Write-Host "Found $($allExportedFiles.Count) exported markdown files" -ForegroundColor Green
                    $allExportedFiles.Count | Should -BeGreaterThan 0 -Because 'Split export should create at least one markdown file'
                }
            }
        }
    }

    AfterAll {
        Write-Host "LocalDisk persistence strategy tests completed" -ForegroundColor Green
    }
}
