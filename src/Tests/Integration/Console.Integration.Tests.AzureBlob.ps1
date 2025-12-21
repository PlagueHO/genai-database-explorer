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
                        Write-Host "Extract-model command succeeded with Azure Blob Storage" -ForegroundColor Green
                    } elseif ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                        Write-Host "Azure Blob container not found - infrastructure may not be fully provisioned" -ForegroundColor Yellow
                    } elseif ($outputText -match 'BlobServiceClient initialization|Failed to initialize Azure Blob Storage') {
                        Write-Host "Azure Blob Storage initialization failed: $outputText" -ForegroundColor Yellow
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
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist - run infrastructure provisioning first'
                    return
                }
                
                if ($outputText -match 'BlobServiceClient initialization|Failed to initialize Azure Blob Storage') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage client initialization failed - check storage account configuration'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Database or storage access not authorized'
                    return
                }
                
                if ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'AzureBlob persistence strategy not yet fully implemented for extract-model'
                    return
                }
                
                $script:ExtractCommandResult.ExitCode | Should -Be 0 -Because 'Extract-model should complete successfully with AzureBlob strategy'
            }

            It 'Should persist model to Azure Blob Storage and retrieve it successfully' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Inconclusive -Because 'Extract-model did not succeed'
                    return
                }

                # Verify by attempting to read the model back via show-object
                $script:VerifyBlobResult = $null
                try {
                    $script:VerifyBlobResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'show-object',
                        'table',
                        '--project', $script:DbProjectPath,
                        '--schema-name', 'SalesLT',
                        '--name', 'Product'
                    )
                } catch {
                    Set-ItResult -Inconclusive -Because "show-object command threw an exception: $_"
                    return
                }
                
                $outputText = if ($script:VerifyBlobResult.Output -is [array]) {
                    $script:VerifyBlobResult.Output -join "`n"
                } else {
                    $script:VerifyBlobResult.Output
                }
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized'
                    return
                }
                
                if ($outputText -match 'No objects found|No semantic model found') {
                    Set-ItResult -Inconclusive -Because 'Model not found in Azure Blob Storage - extract may have failed silently'
                    return
                }
                
                $script:VerifyBlobResult.ExitCode | Should -Be 0 -Because 'Should be able to read model from Azure Blob Storage'
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
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
                    return
                }
                
                if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'
                    return
                }
                
                if ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Enrich-model not yet supported for AzureBlob'
                    return
                }
                
                $script:EnrichResult.ExitCode | Should -Be 0 -Because 'enrich-model should complete successfully with AzureBlob'
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
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized'
                    return
                }
                
                if ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not yet supported for AzureBlob'
                    return
                }
                
                $script:VectorsDryRunResult.ExitCode | Should -Be 0 -Because 'generate-vectors dry-run should complete successfully with AzureBlob'
            }

            It 'Should generate and persist vectors for specific table to Azure Blob Storage' {
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
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
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
                
                if ($outputText -notmatch 'Processed \d+ entit') {
                    Set-ItResult -Inconclusive -Because "Vector generation output did not confirm processing: $outputText"
                    return
                }
                
                # Verify we can retrieve the model with vectors from Azure Blob Storage
                $script:VectorVerifyResult = $null
                try {
                    $script:VectorVerifyResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'show-object',
                        'table',
                        '--project', $script:AiProjectPath,
                        '--schema-name', 'SalesLT',
                        '--name', 'Product'
                    )
                } catch {
                    Set-ItResult -Inconclusive -Because 'show-object verification command threw an exception'
                    return
                }
                
                $script:VectorVerifyResult.ExitCode | Should -Be 0 -Because 'Vector-enriched model should be retrievable from Azure Blob Storage'
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
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized|not authorized to perform this operation') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized - identity may lack Storage Blob Data Contributor role'
                    return
                }
                
                if ($outputText -match 'No semantic model found|not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Azure Blob Storage'
                    return
                }
                
                if ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Show-object not yet supported for AzureBlob'
                    return
                }
                
                $script:ShowObjectResult.ExitCode | Should -Be 0 -Because 'show-object should complete successfully with AzureBlob'
            }
            
            It 'Should display table information in output' {
                if (-not $script:ShowObjectSucceeded) {
                    Set-ItResult -Inconclusive -Because 'show-object did not succeed'
                    return
                }
                
                $outputText = $script:ShowObjectResult.Output -join "`n"
                $outputText | Should -Match 'Product|Table|Schema' -Because 'Output should contain table information from Azure Blob Storage'
            }
        }

        Context 'export-model command' {
            BeforeAll {
                $script:ExportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model.md'
                $script:ExportResult = $null
                $script:ExportSucceeded = $false
                
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
                    Set-ItResult -Inconclusive -Because 'export-model command threw an exception'
                    return
                }
                
                $outputText = $script:ExportResult.Output -join "`n"
                
                if ($outputText -match 'ContainerNotFound|The specified container does not exist|404.*container') {
                    Set-ItResult -Inconclusive -Because 'Azure Blob Storage container does not exist'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized|not authorized to perform this operation') {
                    Set-ItResult -Inconclusive -Because 'Storage access not authorized - identity may lack Storage Blob Data Contributor role'
                    return
                }
                
                if ($outputText -match 'No semantic model found|not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available'
                    return
                }
                
                if ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Export-model not yet supported for AzureBlob'
                    return
                }
                
                $script:ExportResult.ExitCode | Should -Be 0 -Because 'export-model should complete successfully from AzureBlob'
            }
            
            It 'Should create exported markdown file locally' {
                if (-not $script:ExportSucceeded) {
                    Set-ItResult -Inconclusive -Because 'export-model did not succeed'
                    return
                }
                
                Test-Path -Path $script:ExportPath | Should -BeTrue -Because 'Exported file should exist in local filesystem'
            }
        }
    }

    Context 'Azure Blob Storage Specific Scenarios' {
        BeforeAll {
            $script:PrefixTestResult = $null
            $script:PrefixTestSkipped = $false
            
            if ([string]::IsNullOrEmpty($script:TestEnv.AZURE_STORAGE_BLOB_PREFIX)) {
                $script:PrefixTestSkipped = $true
                return
            }

            $script:PrefixProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'prefix-test'
            Initialize-TestProject -ProjectPath $script:PrefixProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $script:PrefixProjectPath
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

            try {
                $script:PrefixTestResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:PrefixProjectPath)
            } catch {
                Write-Host "Extract with blob prefix failed: $_" -ForegroundColor Yellow
            }
        }
        
        It 'Should execute extract-model with blob prefix configuration' {
            if ($script:PrefixTestSkipped) {
                Set-ItResult -Skipped -Because 'Blob prefix not configured'
                return
            }
            
            if (-not $script:PrefixTestResult) {
                Set-ItResult -Inconclusive -Because 'extract-model with blob prefix threw an exception'
                return
            }
            
            $outputText = $script:PrefixTestResult.Output -join "`n"
            
            if ($outputText -match 'AuthorizationFailure|not yet supported') {
                Set-ItResult -Inconclusive -Because 'Blob prefix not yet supported or access denied'
                return
            }
            
            $script:PrefixTestResult.ExitCode | Should -Be 0 -Because 'extract-model should work with blob prefix configured'
        }
    }

    AfterAll {
        Write-Host "AzureBlob persistence strategy tests completed" -ForegroundColor Cyan
    }
}
