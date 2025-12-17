<#
    .SYNOPSIS
        Common integration tests for GenAI Database Explorer Console Application
        These tests MUST pass for ALL persistence strategies (LocalDisk, AzureBlob, CosmosDb)

    .DESCRIPTION
        This test suite contains only tests that are universal across all persistence strategies.
        Tests here validate core functionality that should work identically regardless of where
        semantic models are stored. If a test doesn't apply to all strategies, it belongs in
        a strategy-specific test file.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key
        - PERSISTENCE_STRATEGY: One of 'LocalDisk', 'AzureBlob', 'CosmosDb'
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

# Ensure discovery-safe defaults for script-scoped variables
$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false
$script:PersistenceStrategy = (Get-Variable -Name 'PersistenceStrategy' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? 'LocalDisk'

Describe 'GenAI Database Explorer Console Application - Common Tests' {
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
                [string]$PersistenceStrategy,
                [string]$TestFilter
            )
            
            $resolvedStrategy = Get-ParameterOrEnvironment -ParameterValue $PersistenceStrategy -EnvironmentName 'PERSISTENCE_STRATEGY' -DefaultValue 'LocalDisk'
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
                AZURE_COSMOS_DB_ACCOUNT_ENDPOINT = $env:AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AZURE_COSMOS_DB_DATABASE_NAME = $env:AZURE_COSMOS_DB_DATABASE_NAME
                AZURE_COSMOS_DB_MODELS_CONTAINER = $env:AZURE_COSMOS_DB_MODELS_CONTAINER
                AZURE_COSMOS_DB_ENTITIES_CONTAINER = $env:AZURE_COSMOS_DB_ENTITIES_CONTAINER
                PERSISTENCE_STRATEGY = $resolvedStrategy
            }
            
            $optionalVars = $environmentVars.Keys
            foreach ($varName in $optionalVars) {
                if ([string]::IsNullOrEmpty($environmentVars[$varName])) {
                    Write-Verbose "Environment variable not set: $varName" -Verbose
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
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-integration-test-" + [Guid]::NewGuid().ToString('N'))
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
        $testConfig = Initialize-TestEnvironment -PersistenceStrategy $PersistenceStrategy -TestFilter $TestFilter
        
        $script:PersistenceStrategy = $testConfig.PersistenceStrategy
        $script:NoAzureMode = $testConfig.NoAzureMode
        $script:TestEnv = $testConfig.Environment
        
        Write-Host "Common Tests - Using persistence strategy: $($script:PersistenceStrategy)" -ForegroundColor Cyan
        
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

    Context 'Project Management Commands' {
        Context 'init-project command' {
            Context 'When initializing a new project and project path does not exist' {
                BeforeAll {
                    $script:NewProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'new-init-test'
                    $script:InitResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('init-project', '--project', $script:NewProjectPath)
                }

                AfterAll {
                    if (Test-Path -Path $script:NewProjectPath) {
                        Remove-Item -Path $script:NewProjectPath -Recurse -Force
                    }
                }

                It 'Should create proper project structure and settings.json' {
                    $script:InitResult.ExitCode | Should -Be 0 -Because 'init-project should succeed for non-existent path'
                    
                    Test-Path -Path $script:NewProjectPath | Should -BeTrue -Because 'Project directory should be created'
                    
                    $settingsPath = Join-Path -Path $script:NewProjectPath -ChildPath 'settings.json'
                    Test-Path -Path $settingsPath | Should -BeTrue -Because 'settings.json should be created'
                    
                    $settings = Get-Content -Path $settingsPath | ConvertFrom-Json
                    $settings.SettingsVersion | Should -Not -BeNullOrEmpty -Because 'Settings should have version'
                    $settings.Database | Should -Not -BeNullOrEmpty -Because 'Settings should have Database section'
                    $settings.OpenAIService | Should -Not -BeNullOrEmpty -Because 'Settings should have OpenAIService section'
                }
            }

            Context 'When initializing a new project and project path already exists and is empty' {
                BeforeAll {
                    $script:EmptyProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'empty-init-test'
                    New-Item -ItemType Directory -Path $script:EmptyProjectPath -Force | Out-Null
                }

                AfterAll {
                    if (Test-Path -Path $script:EmptyProjectPath) {
                        Remove-Item -Path $script:EmptyProjectPath -Recurse -Force
                    }
                }

                It 'Should handle existing empty directory gracefully' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('init-project', '--project', $script:EmptyProjectPath)
                    $result.ExitCode | Should -Be 0 -Because 'init-project should succeed for empty existing directory'
                }
            }
        }
    }

    Context 'CLI Interface and Error Handling' {
        Context 'CLI help and error handling' {
            Context 'When requesting help information' {
                It 'Should display help information when --help flag is used' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('--help')
                    $result.ExitCode | Should -Be 0 -Because 'Help command should succeed'
                    $outputText = $result.Output -join "`n"
                    $outputText | Should -Match 'GenAI Database Explorer|Usage:|Commands:' -Because 'Help should contain usage information'
                }
            }

            Context 'When using invalid commands' {
                It 'Should return non-zero exit code for invalid command' {
                    $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('invalid-command-xyz')
                    $result.ExitCode | Should -Not -Be 0 -Because 'Invalid command should fail'
                }
            }
        }
    }

    AfterAll {
        Write-Host "Common tests completed for persistence strategy: $($script:PersistenceStrategy)" -ForegroundColor Cyan
    }
}
