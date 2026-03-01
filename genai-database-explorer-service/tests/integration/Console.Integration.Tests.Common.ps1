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
        # Initialize test configuration using shared helpers
        $testConfig = Initialize-TestEnvironment -PersistenceStrategy $PersistenceStrategy -TestFilter $TestFilter

        $script:PersistenceStrategy = $testConfig.PersistenceStrategy
        $script:NoAzureMode = $testConfig.NoAzureMode
        $script:TestEnv = $testConfig.Environment

        Write-Host "Common Tests - Using persistence strategy: $($script:PersistenceStrategy)" -ForegroundColor Cyan

        Test-RequiredEnvironmentVariables -Environment $script:TestEnv -NoAzureMode $script:NoAzureMode -PersistenceStrategy $script:PersistenceStrategy

        $consoleAppPath = Resolve-ConsoleAppPath

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
                    $settings.MicrosoftFoundry | Should -Not -BeNullOrEmpty -Because 'Settings should have MicrosoftFoundry section'
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

            Context 'When initializing a new project with database settings overrides' {
                BeforeAll {
                    $script:DbOverridePath = Join-Path -Path $script:BaseProjectPath -ChildPath 'db-override-test'
                    $script:DbOverrideResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'init-project',
                        '--project', $script:DbOverridePath,
                        '--database-name', 'TestDatabase',
                        '--database-connection-string', 'Server=testserver;Database=TestDB;Trusted_Connection=True;',
                        '--database-schema', 'SalesLT'
                    )
                }

                AfterAll {
                    if (Test-Path -Path $script:DbOverridePath) {
                        Remove-Item -Path $script:DbOverridePath -Recurse -Force
                    }
                }

                It 'Should succeed and create project with overridden database settings' {
                    $script:DbOverrideResult.ExitCode | Should -Be 0 -Because 'init-project with database overrides should succeed'

                    $settingsPath = Join-Path -Path $script:DbOverridePath -ChildPath 'settings.json'
                    Test-Path -Path $settingsPath | Should -BeTrue -Because 'settings.json should be created'

                    $settings = Get-Content -Path $settingsPath -Raw | ConvertFrom-Json
                    $settings.Database.Name | Should -Be 'TestDatabase' -Because 'Database name should be overridden'
                    $settings.Database.ConnectionString | Should -Be 'Server=testserver;Database=TestDB;Trusted_Connection=True;' -Because 'Connection string should be overridden'
                    $settings.Database.Schema | Should -Be 'SalesLT' -Because 'Schema should be overridden'
                }
            }

            Context 'When initializing a new project with foundry settings overrides' {
                BeforeAll {
                    $script:FoundryOverridePath = Join-Path -Path $script:BaseProjectPath -ChildPath 'foundry-override-test'
                    $script:FoundryOverrideResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'init-project',
                        '--project', $script:FoundryOverridePath,
                        '--foundry-endpoint', 'https://mytest.services.ai.azure.com/',
                        '--foundry-chat-deployment', 'gpt-5-2-chat',
                        '--foundry-embedding-deployment', 'text-embedding-3-large'
                    )
                }

                AfterAll {
                    if (Test-Path -Path $script:FoundryOverridePath) {
                        Remove-Item -Path $script:FoundryOverridePath -Recurse -Force
                    }
                }

                It 'Should succeed and create project with overridden foundry settings' {
                    $script:FoundryOverrideResult.ExitCode | Should -Be 0 -Because 'init-project with foundry overrides should succeed'

                    $settingsPath = Join-Path -Path $script:FoundryOverridePath -ChildPath 'settings.json'
                    $settings = Get-Content -Path $settingsPath -Raw | ConvertFrom-Json
                    $settings.MicrosoftFoundry.Default.Endpoint | Should -Be 'https://mytest.services.ai.azure.com/' -Because 'Foundry endpoint should be overridden'
                    $settings.MicrosoftFoundry.ChatCompletion.DeploymentName | Should -Be 'gpt-5-2-chat' -Because 'Chat deployment should be overridden'
                    $settings.MicrosoftFoundry.Embedding.DeploymentName | Should -Be 'text-embedding-3-large' -Because 'Embedding deployment should be overridden'
                }
            }

            Context 'When initializing a new project with persistence strategy override' {
                BeforeAll {
                    $script:PersistenceOverridePath = Join-Path -Path $script:BaseProjectPath -ChildPath 'persistence-override-test'
                    $script:PersistenceOverrideResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'init-project',
                        '--project', $script:PersistenceOverridePath,
                        '--persistence-strategy', 'AzureBlob'
                    )
                }

                AfterAll {
                    if (Test-Path -Path $script:PersistenceOverridePath) {
                        Remove-Item -Path $script:PersistenceOverridePath -Recurse -Force
                    }
                }

                It 'Should succeed and create project with overridden persistence strategy' {
                    $script:PersistenceOverrideResult.ExitCode | Should -Be 0 -Because 'init-project with persistence strategy override should succeed'

                    $settingsPath = Join-Path -Path $script:PersistenceOverridePath -ChildPath 'settings.json'
                    $settings = Get-Content -Path $settingsPath -Raw | ConvertFrom-Json
                    $settings.SemanticModel.PersistenceStrategy | Should -Be 'AzureBlob' -Because 'Persistence strategy should be overridden'
                }
            }

            Context 'When initializing a new project with invalid settings overrides' {
                BeforeAll {
                    $script:InvalidOverridePath = Join-Path -Path $script:BaseProjectPath -ChildPath 'invalid-override-test'
                    $script:InvalidOverrideResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'init-project',
                        '--project', $script:InvalidOverridePath,
                        '--persistence-strategy', 'InvalidStrategy'
                    )
                }

                AfterAll {
                    if (Test-Path -Path $script:InvalidOverridePath) {
                        Remove-Item -Path $script:InvalidOverridePath -Recurse -Force
                    }
                }

                It 'Should return non-zero exit code for invalid settings override' {
                    $script:InvalidOverrideResult.ExitCode | Should -Not -Be 0 -Because 'init-project with invalid persistence strategy should fail'
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
