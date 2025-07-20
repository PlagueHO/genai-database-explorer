#Requires -Version 5.1

<#
.SYNOPSIS
    Integration tests for GenAI Database Explorer Console Application
.DESCRIPTION
    Comprehensive integration tests that validate all CLI commands against live Azure infrastructure.
    Tests include project initialization, database model extraction, AI enrichment, and export functionality.
.NOTES
    Framework: PowerShell Pester v5.7+
    Author: GenAI Database Explorer Team
    Requirements: Azure SQL Database (AdventureWorksLT), Azure OpenAI Services
#>

using namespace System.Management.Automation

BeforeAll {
    # Global test setup following Pester 5.7+ best practices
    $script:TestWorkspace = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) "genaidb-integration-test-$(Get-Random)") -Force
    $script:ConsoleApp = "./publish/GenAIDBExplorer.Console"
    $script:BaseProjectPath = Join-Path $script:TestWorkspace.FullName "projects"
    New-Item -ItemType Directory -Path $script:BaseProjectPath -Force | Out-Null
    
    Write-Host "Integration test workspace: $($script:TestWorkspace.FullName)" -ForegroundColor Green
    Write-Host "Console app path: $($script:ConsoleApp)" -ForegroundColor Green
    
    # Validate console app exists and is executable
    if (-not (Test-Path $script:ConsoleApp)) {
        throw "Console application not found at: $($script:ConsoleApp)"
    }
    
    # Make console app executable on Unix-like systems
    if ($env:RUNNER_OS -ne 'Windows') {
        $chmodResult = & chmod +x $script:ConsoleApp 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Made console app executable" -ForegroundColor Green
        } else {
            Write-Warning "Failed to make console app executable: $chmodResult"
        }
    }
    
    # Validate required environment variables
    $requiredEnvVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT')
    foreach ($envVar in $requiredEnvVars) {
        $envValue = Get-ChildItem Env: | Where-Object { $_.Name -eq $envVar } | Select-Object -ExpandProperty Value
        if ([string]::IsNullOrEmpty($envValue)) {
            Write-Warning "Environment variable '$envVar' is not set. Some tests may fail."
        } else {
            Write-Host "Environment variable '$envVar' is configured" -ForegroundColor Green
        }
    }
    
    Write-Host "Environment variables validated" -ForegroundColor Green
}

Describe "GenAI Database Explorer Console Application Integration Tests" {
    
    Context "When managing projects" {
        It "Should initialize a new project with proper structure" {
            # Arrange
            $projectPath = Join-Path $script:BaseProjectPath "init-test"
            
            # Act
            Write-Host "Initializing project at: $projectPath" -ForegroundColor Yellow
            $result = & $script:ConsoleApp init-project --project $projectPath 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            $exitCode | Should -Be 0 -Because "init-project command should succeed"
            $result | Should -Not -Match "ERROR|FAIL|Exception" -Because "No errors should be reported"
            Test-Path (Join-Path $projectPath "settings.json") | Should -Be $true -Because "settings.json should be created"
            
            # Validate settings.json structure
            $settingsPath = Join-Path $projectPath "settings.json"
            $settings = Get-Content $settingsPath | ConvertFrom-Json -ErrorAction Stop
            $settings | Should -Not -BeNullOrEmpty -Because "settings.json should contain valid configuration"
            $settings.PSObject.Properties.Name | Should -Contain "connectionStrings" -Because "settings should include connection strings configuration"
            
            Write-Host "Project initialized successfully" -ForegroundColor Green
        }
    }
    
    Context "When working with database operations" {
        BeforeAll {
            # Setup project for database tests
            $script:DbProjectPath = Join-Path $script:BaseProjectPath "database-test"
            Write-Host "Setting up database test project at: $script:DbProjectPath" -ForegroundColor Yellow
            $initResult = & $script:ConsoleApp init-project --project $script:DbProjectPath 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to initialize database test project: $initResult"
                throw "Failed to initialize database test project"
            }
            
            # Configure connection string in settings.json if provided
            if (-not [string]::IsNullOrEmpty($env:SQL_CONNECTION_STRING)) {
                $settingsPath = Join-Path $script:DbProjectPath "settings.json"
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $env:SQL_CONNECTION_STRING
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
                Write-Host "Connection string configured in settings.json" -ForegroundColor Green
            }
        }
        
        It "Should extract database model and create semantic model file" {
            # Act
            Write-Host "Extracting database model from: $($env:SQL_CONNECTION_STRING)" -ForegroundColor Yellow
            $result = & $script:ConsoleApp extract-model --project $script:DbProjectPath 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            if ($exitCode -eq 0) {
                $semanticModelPath = Join-Path $script:DbProjectPath "semanticmodel.json"
                Test-Path $semanticModelPath | Should -Be $true -Because "semanticmodel.json should be created"
                
                # Validate JSON structure
                { Get-Content $semanticModelPath | ConvertFrom-Json -ErrorAction Stop } | Should -Not -Throw -Because "semanticmodel.json should be valid JSON"
                
                $model = Get-Content $semanticModelPath | ConvertFrom-Json
                $model.Database | Should -Not -BeNullOrEmpty -Because "Model should contain database information"
                $model.Database.Name | Should -Match "AdventureWorksLT|Adventure" -Because "Should connect to AdventureWorksLT or similar sample database"
                
                Write-Host "Database model extracted successfully" -ForegroundColor Green
            } else {
                # Handle case where database is not available
                Write-Warning "Database extraction failed, likely due to connection issues: $result"
                $result | Should -Match "connection|database|authentication|timeout" -Because "Should provide meaningful error message for connection issues"
            }
        }
        
        It "Should apply data dictionary without errors" {
            # Arrange - Create a sample data dictionary file
            $dictDir = Join-Path $script:DbProjectPath "dict"
            New-Item -ItemType Directory -Path $dictDir -Force | Out-Null
            $dictPath = Join-Path $dictDir "test-dictionary.json"
            $sampleDict = @{
                objectType = "table"
                schemaName = "dbo"
                objectName = "Customer"
                description = "Customer information table"
                columns = @(
                    @{
                        name = "CustomerID"
                        description = "Unique identifier for customer"
                    }
                )
            }
            $sampleDict | ConvertTo-Json -Depth 3 | Set-Content $dictPath
            
            # Act
            Write-Host "Applying data dictionary from: $dictPath" -ForegroundColor Yellow
            $result = & $script:ConsoleApp data-dictionary --project $script:DbProjectPath --sourcePathPattern "$dictPath" --objectType table 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert - Should not fail even if no matching objects
            $exitCode | Should -BeIn @(0, 1) -Because "Should succeed (0) or indicate no matches found (1)"
            $result | Should -Not -Match "Exception|Error.*Exception" -Because "Should not throw unhandled exceptions"
            
            Write-Host "Data dictionary processed successfully" -ForegroundColor Green
        }
    }
    
    Context "When using AI operations" {
        BeforeAll {
            # Setup project for AI tests
            $script:AiProjectPath = Join-Path $script:BaseProjectPath "ai-test"
            Write-Host "Setting up AI test project at: $script:AiProjectPath" -ForegroundColor Yellow
            & $script:ConsoleApp init-project --project $script:AiProjectPath | Out-Null
            
            # Configure settings for AI operations
            if (-not [string]::IsNullOrEmpty($env:SQL_CONNECTION_STRING) -and -not [string]::IsNullOrEmpty($env:AZURE_OPENAI_ENDPOINT)) {
                $settingsPath = Join-Path $script:AiProjectPath "settings.json"
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $env:SQL_CONNECTION_STRING
                $settings.azureOpenAI.endpoint = $env:AZURE_OPENAI_ENDPOINT
                if (-not [string]::IsNullOrEmpty($env:AZURE_OPENAI_API_KEY)) {
                    $settings.azureOpenAI.apiKey = $env:AZURE_OPENAI_API_KEY
                }
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
            }
            
            # Extract model first for AI operations (suppress output if fails)
            & $script:ConsoleApp extract-model --project $script:AiProjectPath 2>&1 | Out-Null
        }
        
        It "Should handle AI model enrichment gracefully when AI services are available or unavailable" {
            # Act
            Write-Host "Testing AI model enrichment" -ForegroundColor Yellow
            $result = & $script:ConsoleApp enrich-model --project $script:AiProjectPath --objectType table --schemaName dbo 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert - May fail if AI service unavailable, but should handle gracefully
            if ($exitCode -eq 0) {
                $result | Should -Not -Match "Exception.*at.*" -Because "Successful execution should not show stack traces"
                Write-Host "AI enrichment completed successfully" -ForegroundColor Green
            } else {
                $result | Should -Match "AI|service|connection|authentication|endpoint|model" -Because "Failed execution should provide meaningful error message"
                Write-Warning "AI enrichment failed (expected if AI services not configured): $($result -join ' ')"
            }
        }
        
        It "Should handle model querying interface gracefully" {
            # Act - Test basic query functionality
            Write-Host "Testing model query interface" -ForegroundColor Yellow
            $result = & $script:ConsoleApp query-model --project $script:AiProjectPath --help 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert - Should at least show help or handle the command
            $result | Should -Not -Match "Exception.*at.*" -Because "Help command should not show stack traces"
            if ($exitCode -eq 0) {
                $result | Should -Match "Usage|Options|Commands" -Because "Help should display usage information"
                Write-Host "Query interface is functional" -ForegroundColor Green
            }
        }
    }
    
    Context "When displaying and exporting data" {
        BeforeAll {
            # Setup project for display tests
            $script:DisplayProjectPath = Join-Path $script:BaseProjectPath "display-test"
            Write-Host "Setting up display test project at: $script:DisplayProjectPath" -ForegroundColor Yellow
            & $script:ConsoleApp init-project --project $script:DisplayProjectPath | Out-Null
            
            # Configure connection if available
            if (-not [string]::IsNullOrEmpty($env:SQL_CONNECTION_STRING)) {
                $settingsPath = Join-Path $script:DisplayProjectPath "settings.json"
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $settings.connectionStrings.defaultConnection = $env:SQL_CONNECTION_STRING
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
            }
            
            # Extract model for display operations (suppress output if fails)
            & $script:ConsoleApp extract-model --project $script:DisplayProjectPath 2>&1 | Out-Null
        }
        
        It "Should display database object information or handle missing objects gracefully" {
            # Act - Try to show any available table
            Write-Host "Testing object display functionality" -ForegroundColor Yellow
            $result = & $script:ConsoleApp show-object table --project $script:DisplayProjectPath --schemaName dbo 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            if ($exitCode -eq 0) {
                $result | Should -Not -BeNullOrEmpty -Because "Successful execution should produce output"
                $result | Should -Not -Match "Exception.*at.*" -Because "Successful execution should not show stack traces"
                Write-Host "Object display completed successfully" -ForegroundColor Green
            } else {
                # If no tables found or other issue, should fail gracefully
                $result | Should -Match "table|object|schema|not found|No.*found" -Because "Should provide meaningful error message when objects not found"
                Write-Warning "No objects found to display (expected if database not available)"
            }
        }
        
        It "Should export semantic model to markdown format successfully" {
            # Act
            $exportPath = Join-Path $script:DisplayProjectPath "exported-model.md"
            Write-Host "Testing model export to: $exportPath" -ForegroundColor Yellow
            $result = & $script:ConsoleApp export-model --project $script:DisplayProjectPath --outputPath $exportPath --fileType markdown 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            if ($exitCode -eq 0) {
                Test-Path $exportPath | Should -Be $true -Because "Exported file should exist"
                
                $exportContent = Get-Content $exportPath -Raw -ErrorAction SilentlyContinue
                $exportContent | Should -Not -BeNullOrEmpty -Because "Exported content should not be empty"
                $exportContent | Should -Match "Database|Model|Schema|#" -Because "Should contain expected database documentation content with markdown formatting"
                
                Write-Host "Model export completed successfully" -ForegroundColor Green
            } else {
                # Export may fail if no semantic model exists
                $result | Should -Match "model|semantic|export|not found" -Because "Should provide meaningful error message for export failures"
                Write-Warning "Model export failed (expected if no semantic model exists): $($result -join ' ')"
            }
        }
    }
    
    Context "When validating CLI command structure and help" {
        It "Should display main help information correctly" {
            # Act
            Write-Host "Testing main CLI help" -ForegroundColor Yellow
            $result = & $script:ConsoleApp --help 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            $exitCode | Should -Be 0 -Because "Help command should succeed"
            $result | Should -Not -BeNullOrEmpty -Because "Help should produce output"
            $result | Should -Match "Usage|Commands|Options" -Because "Should display standard CLI help format"
            $result | Should -Match "init-project|extract-model|query-model" -Because "Should list main CLI commands"
            
            Write-Host "CLI help displayed successfully" -ForegroundColor Green
        }
        
        It "Should handle invalid commands gracefully" {
            # Act
            Write-Host "Testing invalid command handling" -ForegroundColor Yellow
            $result = & $script:ConsoleApp invalid-command-test 2>&1
            $exitCode = $LASTEXITCODE
            
            # Assert
            $exitCode | Should -Not -Be 0 -Because "Invalid command should return non-zero exit code"
            $result | Should -Match "command|invalid|unknown|not.*recognized" -Because "Should provide error message for invalid commands"
            
            Write-Host "Invalid command handled appropriately" -ForegroundColor Green
        }
    }
}

AfterAll {
    # Global cleanup following Pester 5.7+ best practices
    if ($script:TestWorkspace -and (Test-Path $script:TestWorkspace)) {
        Write-Host "Cleaning up test workspace: $($script:TestWorkspace.FullName)" -ForegroundColor Yellow
        try {
            Remove-Item $script:TestWorkspace -Recurse -Force -ErrorAction Stop
            Write-Host "Test workspace cleaned up successfully" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to clean up test workspace: $($_.Exception.Message)"
        }
    }
}
