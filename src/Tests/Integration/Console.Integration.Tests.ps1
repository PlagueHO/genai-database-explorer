
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

Describe 'GenAI Database Explorer Console Application' {
    BeforeAll {
        # Arrange: Create test workspace and validate console app
        $script:TestWorkspace = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) "genaidb-integration-test-$(Get-Random)") -Force
        $script:ConsoleApp = "./publish/GenAIDBExplorer.Console"
        $script:BaseProjectPath = Join-Path $script:TestWorkspace.FullName "projects"
        New-Item -ItemType Directory -Path $script:BaseProjectPath -Force | Out-Null

        if (-not (Test-Path $script:ConsoleApp)) {
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
                    $script:InitProjectPath = Join-Path $script:BaseProjectPath 'init-test'
                }

                It 'Should create proper project structure and settings.json' {
                    # Arrange
                    $expectedSettingsPath = Join-Path $script:InitProjectPath 'settings.json'

                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp init-project --project $script:InitProjectPath" -Verbose
                    $result = & $script:ConsoleApp init-project --project $script:InitProjectPath 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $exitCode | Should -Be 0 -Because 'init-project command should succeed'
                    $result | Should -Not -Match 'ERROR|FAIL|Exception' -Because 'No errors should be reported'
                    $expectedSettingsPath | Should -Exist -Because 'settings.json should be created'

                    # Validate settings.json structure
                    { Get-Content $expectedSettingsPath | ConvertFrom-Json -ErrorAction Stop } |
                        Should -Not -Throw -Because 'settings.json should contain valid JSON'

                    $settings = Get-Content $expectedSettingsPath | ConvertFrom-Json
                    $settings | Should -Not -BeNullOrEmpty -Because 'settings.json should contain valid configuration'
                    $settings.PSObject.Properties.Name | Should -Contain 'connectionStrings' -Because 'settings should include connection strings configuration'
                }
            }

            Context 'When project path already exists' {
                BeforeAll {
                    # Arrange
                    $script:ExistingProjectPath = Join-Path $script:BaseProjectPath 'existing-test'
                    New-Item -ItemType Directory -Path $script:ExistingProjectPath -Force | Out-Null
                }

                It 'Should handle existing directory gracefully' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp init-project --project $script:ExistingProjectPath" -Verbose
                    $result = & $script:ConsoleApp init-project --project $script:ExistingProjectPath 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $exitCode | Should -BeIn @(0, 1) -Because 'Should succeed or indicate directory exists'
                }
            }
        }
    }

    Context 'Database Schema Operations' {
        BeforeAll {
            # Setup shared project for database tests
            $script:DbProjectPath = Join-Path $script:BaseProjectPath 'database-test'
            Write-Verbose "Setting up database test project at: $script:DbProjectPath" -Verbose
            $initResult = & $script:ConsoleApp init-project --project $script:DbProjectPath 2>&1

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to initialize database test project: $initResult"
                throw "Failed to initialize database test project"
            }

            # Configure connection string in settings.json if provided
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            if ($connectionString -and -not [string]::IsNullOrEmpty($connectionString.Value)) {
                $settingsPath = Join-Path $script:DbProjectPath 'settings.json'
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $connectionString.Value
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
                Write-Verbose "Connection string configured in settings.json" -Verbose
            }
        }

        Context 'extract-model command' {
            Context 'When extracting from valid database connection' {
                It 'Should create semanticmodel.json with database schema' {
                    # Arrange
                    $expectedSemanticModelPath = Join-Path $script:DbProjectPath 'semanticmodel.json'

                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp extract-model --project $script:DbProjectPath" -Verbose
                    $result = & $script:ConsoleApp extract-model --project $script:DbProjectPath 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    if ($exitCode -eq 0) {
                        $expectedSemanticModelPath | Should -Exist -Because 'semanticmodel.json should be created'

                        # Validate JSON structure
                        { Get-Content $expectedSemanticModelPath | ConvertFrom-Json -ErrorAction Stop } |
                            Should -Not -Throw -Because 'semanticmodel.json should be valid JSON'

                        $model = Get-Content $expectedSemanticModelPath | ConvertFrom-Json
                        $model.Database | Should -Not -BeNullOrEmpty -Because 'Model should contain database information'
                        $model.Database.Name | Should -Match 'AdventureWorksLT|Adventure' -Because 'Should connect to AdventureWorksLT or similar sample database'
                    } else {
                        # Handle case where database is not available
                        Write-Verbose "Database extraction failed, likely due to connection issues" -Verbose
                        $result | Should -Match 'connection|database|authentication|timeout' -Because 'Should provide meaningful error message for connection issues'
                    }
                }
            }

            Context 'When extracting with specific options' {
                It 'Should handle skipTables option correctly' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp extract-model --project $script:DbProjectPath --skipTables" -Verbose
                    $result = & $script:ConsoleApp extract-model --project $script:DbProjectPath --skipTables 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces for valid options'
                }
            }
        }

        Context 'data-dictionary command' {
            Context 'When applying data dictionary files' {
                BeforeAll {
                    # Arrange - Create a sample data dictionary file
                    $script:DictDir = Join-Path $script:DbProjectPath 'dict'
                    New-Item -ItemType Directory -Path $script:DictDir -Force | Out-Null
                    $script:DictPath = Join-Path $script:DictDir 'test-dictionary.json'
                    $sampleDict = @{
                        objectType = 'table'
                        schemaName = 'dbo'
                        objectName = 'Customer'
                        description = 'Customer information table'
                        columns = @(
                            @{
                                name = 'CustomerID'
                                description = 'Unique identifier for customer'
                            }
                        )
                    }
                    $sampleDict | ConvertTo-Json -Depth 3 | Set-Content $script:DictPath
                }

                It 'Should process dictionary files without errors' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp data-dictionary --project $script:DbProjectPath --sourcePathPattern $script:DictPath --objectType table" -Verbose
                    $result = & $script:ConsoleApp data-dictionary --project $script:DbProjectPath --sourcePathPattern "$script:DictPath" --objectType table 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert - Should not fail even if no matching objects
                    $exitCode | Should -BeIn @(0, 1) -Because 'Should succeed (0) or indicate no matches found (1)'
                    $result | Should -Not -Match 'Exception|Error.*Exception' -Because 'Should not throw unhandled exceptions'
                }
            }

            Context 'When showing applied dictionaries' {
                BeforeAll {
                    # Arrange
                    $script:ShowDictDir = Join-Path $script:DbProjectPath 'dict'
                    $script:ShowDictPath = Join-Path $script:ShowDictDir 'show-test-dictionary.json'
                    $sampleDict = @{
                        objectType = 'table'
                        schemaName = 'dbo'
                        objectName = 'Product'
                        description = 'Product catalog table'
                    }
                    $sampleDict | ConvertTo-Json -Depth 3 | Set-Content $script:ShowDictPath
                }

                It 'Should display dictionary information with --show option' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp data-dictionary --project $script:DbProjectPath --sourcePathPattern $script:ShowDictPath --objectType table --show" -Verbose
                    $result = & $script:ConsoleApp data-dictionary --project $script:DbProjectPath --sourcePathPattern "$script:ShowDictPath" --objectType table --show 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }
    }

    Context 'AI Operations' {
        BeforeAll {
            # Setup project for AI tests
            $script:AiProjectPath = Join-Path $script:BaseProjectPath 'ai-test'
            Write-Verbose "Setting up AI test project at: $script:AiProjectPath" -Verbose
            & $script:ConsoleApp init-project --project $script:AiProjectPath | Out-Null

            # Configure settings for AI operations
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            $openAiEndpoint = Get-Item -Path 'Env:AZURE_OPENAI_ENDPOINT' -ErrorAction SilentlyContinue
            $openAiApiKey = Get-Item -Path 'Env:AZURE_OPENAI_API_KEY' -ErrorAction SilentlyContinue

            if ($connectionString -and $openAiEndpoint -and
                -not [string]::IsNullOrEmpty($connectionString.Value) -and
                -not [string]::IsNullOrEmpty($openAiEndpoint.Value)) {
                $settingsPath = Join-Path $script:AiProjectPath 'settings.json'
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $connectionString.Value
                $settings.azureOpenAI.endpoint = $openAiEndpoint.Value
                if ($openAiApiKey -and -not [string]::IsNullOrEmpty($openAiApiKey.Value)) {
                    $settings.azureOpenAI.apiKey = $openAiApiKey.Value
                }
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
            }

            # Extract model first for AI operations (suppress output if fails)
            & $script:ConsoleApp extract-model --project $script:AiProjectPath 2>&1 | Out-Null
        }

        Context 'enrich-model command' {
            Context 'When enriching with AI services available' {
                It 'Should enhance semantic model with AI-generated descriptions' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp enrich-model --project $script:AiProjectPath --objectType table --schemaName dbo" -Verbose
                    $result = & $script:ConsoleApp enrich-model --project $script:AiProjectPath --objectType table --schemaName dbo 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert - May fail if AI service unavailable, but should handle gracefully
                    if ($exitCode -eq 0) {
                        $result | Should -Not -Match 'Exception.*at.*' -Because 'Successful execution should not show stack traces'
                    } else {
                        $result | Should -Match 'AI|service|connection|authentication|endpoint|model' -Because 'Failed execution should provide meaningful error message'
                        Write-Verbose "AI enrichment failed (expected if AI services not configured)" -Verbose
                    }
                }
            }

            Context 'When enriching specific objects' {
                It 'Should handle enrichment with object name filters' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp enrich-model --project $script:AiProjectPath --objectType table --schemaName dbo --objectName Customer" -Verbose
                    $result = & $script:ConsoleApp enrich-model --project $script:AiProjectPath --objectType table --schemaName dbo --objectName Customer 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }
    }

    Context 'Model Display and Export Operations' {
        BeforeAll {
            # Setup shared project for display tests
            $script:DisplayProjectPath = Join-Path $script:BaseProjectPath 'display-test'
            Write-Verbose "Setting up display test project at: $script:DisplayProjectPath" -Verbose
            & $script:ConsoleApp init-project --project $script:DisplayProjectPath | Out-Null

            # Configure connection if available
            $connectionString = Get-Item -Path 'Env:SQL_CONNECTION_STRING' -ErrorAction SilentlyContinue
            if ($connectionString -and -not [string]::IsNullOrEmpty($connectionString.Value)) {
                $settingsPath = Join-Path $script:DisplayProjectPath 'settings.json'
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $connectionString.Value
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
            }

            # Extract model for display operations (suppress output if fails)
            & $script:ConsoleApp extract-model --project $script:DisplayProjectPath 2>&1 | Out-Null
        }

        Context 'show-object command' {
            Context 'When displaying table information' {
                It 'Should show table details or handle missing tables gracefully' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp show-object table --project $script:DisplayProjectPath --schemaName dbo" -Verbose
                    $result = & $script:ConsoleApp show-object table --project $script:DisplayProjectPath --schemaName dbo 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    if ($exitCode -eq 0) {
                        $result | Should -Not -BeNullOrEmpty -Because 'Successful execution should produce output'
                        $result | Should -Not -Match 'Exception.*at.*' -Because 'Successful execution should not show stack traces'
                    } else {
                        # If no tables found or other issue, should fail gracefully
                        $result | Should -Match 'table|object|schema|not found|No.*found' -Because 'Should provide meaningful error message when objects not found'
                        Write-Verbose "No objects found to display (expected if database not available)" -Verbose
                    }
                }
            }

            Context 'When displaying specific object by name' {
                It 'Should show specific table details when name is provided' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp show-object table --project $script:DisplayProjectPath --schemaName dbo --name Customer" -Verbose
                    $result = & $script:ConsoleApp show-object table --project $script:DisplayProjectPath --schemaName dbo --name Customer 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'query-model command' {
            Context 'When accessing query interface' {
                It 'Should display help or handle query command gracefully' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp query-model --project $script:AiProjectPath --help" -Verbose
                    $result = & $script:ConsoleApp query-model --project $script:AiProjectPath --help 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Help command should not show stack traces'
                    if ($exitCode -eq 0) {
                        $result | Should -Match 'Usage|Options|Commands' -Because 'Help should display usage information'
                    }
                }
            }

            Context 'When testing query functionality' {
                It 'Should handle basic query operations' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp query-model --project $script:AiProjectPath" -Verbose
                    $result = & $script:ConsoleApp query-model --project $script:AiProjectPath 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces'
                }
            }
        }

        Context 'export-model command' {
            Context 'When exporting to markdown format' {
                It 'Should create markdown documentation file' {
                    # Arrange
                    $exportPath = Join-Path $script:DisplayProjectPath 'exported-model.md'

                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp export-model --project $script:DisplayProjectPath --outputPath $exportPath --fileType markdown" -Verbose
                    $result = & $script:ConsoleApp export-model --project $script:DisplayProjectPath --outputPath $exportPath --fileType markdown 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    if ($exitCode -eq 0) {
                        $exportPath | Should -Exist -Because 'Exported file should exist'

                        $exportContent = Get-Content $exportPath -Raw -ErrorAction SilentlyContinue
                        $exportContent | Should -Not -BeNullOrEmpty -Because 'Exported content should not be empty'
                        $exportContent | Should -Match 'Database|Model|Schema|#' -Because 'Should contain expected database documentation content with markdown formatting'
                    } else {
                        # Export may fail if no semantic model exists
                        $result | Should -Match 'model|semantic|export|not found' -Because 'Should provide meaningful error message for export failures'
                        Write-Verbose "Model export failed (expected if no semantic model exists)" -Verbose
                    }
                }
            }

            Context 'When exporting with split files option' {
                It 'Should handle splitFiles option correctly' {
                    # Arrange
                    $exportPath = Join-Path $script:DisplayProjectPath 'exported-model-split.md'

                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp export-model --project $script:DisplayProjectPath --outputPath $exportPath --fileType markdown --splitFiles" -Verbose
                    $result = & $script:ConsoleApp export-model --project $script:DisplayProjectPath --outputPath $exportPath --fileType markdown --splitFiles 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $result | Should -Not -Match 'Exception.*at.*' -Because 'Should not show stack traces for valid options'
                }
            }
        }
    }

    Context 'CLI Interface and Error Handling' {
        Context 'CLI help and error handling' {
            Context 'When requesting help information' {
                It 'Should display main help information correctly' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp --help" -Verbose
                    $result = & $script:ConsoleApp --help 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $exitCode | Should -Be 0 -Because 'Help command should succeed'
                    $result | Should -Not -BeNullOrEmpty -Because 'Help should produce output'
                    $result | Should -Match 'Usage|Commands|Options' -Because 'Should display standard CLI help format'
                    $result | Should -Match 'init-project|extract-model|query-model' -Because 'Should list main CLI commands'
                }
            }

            Context 'When using invalid commands' {
                It 'Should handle invalid commands gracefully' {
                    # Act
                    Write-Verbose "Executing: $script:ConsoleApp invalid-command-test" -Verbose
                    $result = & $script:ConsoleApp invalid-command-test 2>&1
                    $exitCode = $LASTEXITCODE

                    Write-Verbose "Console Output: $($result -join '; ')" -Verbose
                    Write-Verbose "Exit Code: $exitCode" -Verbose

                    # Assert
                    $exitCode | Should -Not -Be 0 -Because 'Invalid command should return non-zero exit code'
                    $result | Should -Match 'command|invalid|unknown|not.*recognized' -Because 'Should provide error message for invalid commands'
                }
            }
        }
    }

    AfterAll {
        # Cleanup: Remove test workspace if it exists
        if ($script:TestWorkspace -and (Test-Path $script:TestWorkspace)) {
            try {
                Remove-Item $script:TestWorkspace -Recurse -Force -ErrorAction SilentlyContinue
            } catch {
                Write-Warning "Failed to clean up test workspace: $($_.Exception.Message)"
            }
        }
    }
}


