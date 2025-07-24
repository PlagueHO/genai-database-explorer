<#
    .SYNOPSIS
        Integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
        Comprehensive integration tests that validate all CLI commands against live Azure infrastructure.
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
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key (optional if using managed identity)

    .OUTPUTS
        Pester test results in NUnitXml format
#>
#Requires -Version 5.1

using namespace System.Management.Automation

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

Describe 'GenAI Database Explorer Console Application' {
    BeforeAll {
        # Arrange: Create test workspace and validate console app
        $script:TestWorkspace = New-Item -ItemType Directory -Path (Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "genaidb-integration-test-$(Get-Random)") -Force
        $script:ConsoleApp = "./publish/GenAIDBExplorer.Console"
        $script:BaseProjectPath = Join-Path -Path $script:TestWorkspace.FullName -ChildPath "projects"
        New-Item -ItemType Directory -Path $script:BaseProjectPath -Force | Out-Null

        if (-not (Test-Path -Path $script:ConsoleApp)) {
            throw "Console application not found at: $($script:ConsoleApp)"
        }

        if ($env:RUNNER_OS -ne 'Windows') {
            & chmod +x $script:ConsoleApp 2>&1 | Out-Null
        }

        $requiredEnvVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT')
        foreach ($envVar in $requiredEnvVars) {
            if ($null -eq (Get-Item -Path "Env:$envVar" -ErrorAction SilentlyContinue) -or
                [string]::IsNullOrEmpty((Get-Item -Path "Env:$envVar" -ErrorAction SilentlyContinue).Value)) {
                Write-Verbose "Environment variable '$envVar' is not set. Some tests may fail." -Verbose
            }
        }
    }

    Context 'Project Management Commands' {
        Context 'init-project command' {
            Context 'When initializing a new project' {
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
                }

                It 'Should create proper project structure and settings.json' {
                    # Arrange
                    $expectedSettingsPath = Join-Path -Path $script:InitProjectPath -ChildPath 'settings.json'

                    # Ensure the target directory does NOT exist (testing new project creation)
                    if (Test-Path -Path $script:InitProjectPath) {
                        Remove-Item -Path $script:InitProjectPath -Recurse -Force -ErrorAction SilentlyContinue
                    }

                    Write-Verbose "Target directory exists before test: $(Test-Path -Path $script:InitProjectPath)" -Verbose

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('init-project', '--project', $script:InitProjectPath)

                    # Assert - Check for specific error patterns that indicate failure
                    if ($commandResult.Output -match 'Could not find a part of the path|Access.*denied|Permission.*denied|Path.*not.*found') {
                        throw "Command failed with path error: $($commandResult.Output)"
                    }

                    $commandResult.ExitCode | Should -Be 0 -Because 'init-project command should succeed'
                    $commandResult.Output | Should -Not -Match 'ERROR|FAIL|Exception|Could not find|Access.*denied' -Because 'No errors should be reported'

                    # Verify project directory was created
                    $script:InitProjectPath | Should -Exist -Because 'Project directory should be created'
                    $expectedSettingsPath | Should -Exist -Because 'settings.json should be created'

                    # Validate settings.json structure
                    { Get-Content -Path $expectedSettingsPath | ConvertFrom-Json -ErrorAction Stop } |
                        Should -Not -Throw -Because 'settings.json should contain valid JSON'

                    $settings = Get-Content -Path $expectedSettingsPath | ConvertFrom-Json
                    $settings | Should -Not -BeNullOrEmpty -Because 'settings.json should contain valid configuration'
                    $settings.PSObject.Properties.Name | Should -Contain 'connectionStrings' -Because 'settings should include connection strings configuration'
                }
            }

            Context 'When project path already exists' {
                BeforeAll {
                    # Arrange
                    $script:ExistingProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'existing-test'
                    New-Item -ItemType Directory -Path $script:ExistingProjectPath -Force | Out-Null
                }

                It 'Should handle existing directory gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('init-project', '--project', $script:ExistingProjectPath)

                    # Assert
                    $commandResult.ExitCode | Should -BeIn @(0, 1) -Because 'Should succeed or indicate directory exists'
                }
            }
        }
    }

    Context 'Database Schema Operations' {
        BeforeAll {
            # Setup shared project for database tests
            $script:DbProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'database-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:DbProjectPath -ConsoleApp $script:ConsoleApp

            if ($projectSetup.ExitCode -ne 0) {
                Write-Error "Failed to initialize database test project: $($projectSetup.InitResult)"
                throw "Failed to initialize database test project"
            }

            # Configure connection string in settings.json if provided
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            if ($connectionString -and -not [string]::IsNullOrEmpty($connectionString.Value)) {
                Set-ProjectSettings -ProjectPath $script:DbProjectPath -ConnectionString $connectionString.Value
            }
        }

        Context 'extract-model command' {
            Context 'When extracting from valid database connection' {
                It 'Should create semanticmodel.json with database schema' {
                    # Arrange
                    $expectedSemanticModelPath = Join-Path -Path $script:DbProjectPath -ChildPath 'semanticmodel.json'

                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('extract-model', '--project', $script:DbProjectPath)

                    # Assert
                    if ($commandResult.ExitCode -eq 0) {
                        $expectedSemanticModelPath | Should -Exist -Because 'semanticmodel.json should be created'

                        # Validate JSON structure
                        { Get-Content -Path $expectedSemanticModelPath | ConvertFrom-Json -ErrorAction Stop } |
                            Should -Not -Throw -Because 'semanticmodel.json should be valid JSON'

                        $model = Get-Content -Path $expectedSemanticModelPath | ConvertFrom-Json
                        $model.Database | Should -Not -BeNullOrEmpty -Because 'Model should contain database information'
                        $model.Database.Name | Should -Match 'AdventureWorksLT|Adventure' -Because 'Should connect to AdventureWorksLT or similar sample database'
                    } else {
                        # Handle case where database is not available
                        Write-Verbose "Database extraction failed, likely due to connection issues" -Verbose
                        $commandResult.Output | Should -Match 'connection|database|authentication|timeout' -Because 'Should provide meaningful error message for connection issues'
                    }
                }
            }

            Context 'When extracting with specific options' {
                It 'Should handle skipTables option correctly' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('extract-model', '--project', $script:DbProjectPath, '--skipTables')

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

                It 'Should process dictionary files without errors' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('data-dictionary', '--project', $script:DbProjectPath, '--sourcePathPattern', $script:DictPath, '--objectType', 'table')

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

                It 'Should display dictionary information with --show option' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('data-dictionary', '--project', $script:DbProjectPath, '--sourcePathPattern', $script:ShowDictPath, '--objectType', 'table', '--show')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }
    }

    Context 'AI Operations' {
        BeforeAll {
            # Setup project for AI tests
            $script:AiProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'ai-test'
            $projectSetup = Initialize-TestProject -ProjectPath $script:AiProjectPath -ConsoleApp $script:ConsoleApp

            if ($projectSetup.ExitCode -ne 0) {
                Write-Warning "Failed to initialize AI test project: $($projectSetup.InitResult)"
            }

            # Configure settings for AI operations
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            $openAiEndpoint = Get-Item -Path 'Env:AZURE_OPENAI_ENDPOINT' -ErrorAction SilentlyContinue
            $openAiApiKey = Get-Item -Path 'Env:AZURE_OPENAI_API_KEY' -ErrorAction SilentlyContinue

            if ($connectionString -and $openAiEndpoint -and
                -not [string]::IsNullOrEmpty($connectionString.Value) -and
                -not [string]::IsNullOrEmpty($openAiEndpoint.Value)) {

                $configParams = @{
                    ProjectPath = $script:AiProjectPath
                    ConnectionString = $connectionString.Value
                    AzureOpenAIEndpoint = $openAiEndpoint.Value
                }

                if ($openAiApiKey -and -not [string]::IsNullOrEmpty($openAiApiKey.Value)) {
                    $configParams.AzureOpenAIApiKey = $openAiApiKey.Value
                }

                Set-ProjectSettings @configParams
            }

            # Extract model first for AI operations (suppress output if fails)
            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            Context 'When enriching with AI services available' {
                It 'Should enhance semantic model with AI-generated descriptions' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('enrich-model', '--project', $script:AiProjectPath, '--objectType', 'table', '--schemaName', 'dbo')

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
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('enrich-model', '--project', $script:AiProjectPath, '--objectType', 'table', '--schemaName', 'dbo', '--objectName', 'Customer')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }
    }

    Context 'Model Display and Export Operations' {
        BeforeAll {
            # Setup shared project for display tests
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleApp | Out-Null

            # Configure connection if available
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            if ($connectionString -and -not [string]::IsNullOrEmpty($connectionString.Value)) {
                Set-ProjectSettings -ProjectPath $script:DisplayProjectPath -ConnectionString $connectionString.Value
            }

            # Extract model for display operations (suppress output if fails)
            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            Context 'When displaying table information' {
                It 'Should show table details or handle missing tables gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('show-object', 'table', '--project', $script:DisplayProjectPath, '--schemaName', 'dbo')

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
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('show-object', 'table', '--project', $script:DisplayProjectPath, '--schemaName', 'dbo', '--name', 'Customer')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'query-model command' {
            Context 'When accessing query interface' {
                It 'Should display help or handle query command gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('query-model', '--project', $script:AiProjectPath, '--help')

                    # Assert
                    $commandResult.Output | Should -Not -Match 'Exception.*at.*' -Because 'Help command should not show stack traces'
                    if ($commandResult.ExitCode -eq 0) {
                        $commandResult.Output | Should -Match 'Usage|Options|Commands' -Because 'Help should display usage information'
                    }
                }
            }

            Context 'When testing query functionality' {
                It 'Should handle basic query operations' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('query-model', '--project', $script:AiProjectPath)

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
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('export-model', '--project', $script:DisplayProjectPath, '--outputPath', $exportPath, '--fileType', 'markdown')

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
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('export-model', '--project', $script:DisplayProjectPath, '--outputPath', $exportPath, '--fileType', 'markdown', '--splitFiles')

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
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('--help')

                    # Assert
                    $commandResult.ExitCode | Should -Be 0 -Because 'Help command should succeed'
                    $commandResult.Output | Should -Not -BeNullOrEmpty -Because 'Help should produce output'
                    $commandResult.Output | Should -Match 'Usage|Commands|Options' -Because 'Should display standard CLI help format'
                    $commandResult.Output | Should -Match 'init-project|extract-model|query-model' -Because 'Should list main CLI commands'
                }
            }

            Context 'When using invalid commands' {
                It 'Should handle invalid commands gracefully' {
                    # Act
                    $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleApp -Arguments @('invalid-command-test')

                    # Assert
                    $commandResult.ExitCode | Should -Not -Be 0 -Because 'Invalid command should return non-zero exit code'
                    $commandResult.Output | Should -Match 'command|invalid|unknown|not.*recognized' -Because 'Should provide error message for invalid commands'
                }
            }
        }
    }

    AfterAll {
        # Cleanup: Remove test workspace if it exists
        if ($script:TestWorkspace -and (Test-Path -Path $script:TestWorkspace)) {
            try {
                Remove-Item -Path $script:TestWorkspace -Recurse -Force -ErrorAction SilentlyContinue
            } catch {
                Write-Warning "Failed to clean up test workspace: $($_.Exception.Message)"
            }
        }
    }
}


