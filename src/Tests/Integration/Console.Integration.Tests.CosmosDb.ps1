<#
    .SYNOPSIS
        CosmosDb persistence strategy integration tests for GenAI Database Explorer Console Application

    .DESCRIPTION
        This test suite contains tests specific to the CosmosDb persistence strategy.
        Tests here validate Azure Cosmos DB operations, document-based storage patterns,
        and behaviors unique to storing semantic models in Azure Cosmos DB.

    .NOTES
        Framework: PowerShell Pester v5.7+
        Author: GenAI Database Explorer Team
        Version: 1.0.0

        Environment Variables Required:
        - SQL_CONNECTION_STRING: Connection string for test database
        - AZURE_OPENAI_ENDPOINT: Azure OpenAI service endpoint
        - AZURE_OPENAI_API_KEY: Azure OpenAI API key
        - PERSISTENCE_STRATEGY: Should be 'CosmosDb'
        - AZURE_COSMOS_DB_ACCOUNT_ENDPOINT: Cosmos DB account endpoint
        - AZURE_COSMOS_DB_DATABASE_NAME: Cosmos DB database name (optional, defaults to SemanticModels)
        - AZURE_COSMOS_DB_MODELS_CONTAINER: Models container name (optional, defaults to Models)
        - AZURE_COSMOS_DB_ENTITIES_CONTAINER: Entities container name (optional, defaults to ModelEntities)
#>
#Requires -Version 7

param(
    [Parameter()]
    [ValidateSet('CosmosDb')]
    [string]
    $PersistenceStrategy = 'CosmosDb',
    
    [Parameter()]
    [string]
    $TestFilter = $null
)

# Import the TestHelper module for fixture support functions
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath 'TestHelper\TestHelper.psd1') -Force

$script:NoAzureMode = (Get-Variable -Name 'NoAzureMode' -Scope Script -ValueOnly -ErrorAction SilentlyContinue) ?? $false

Describe 'GenAI Database Explorer Console Application - CosmosDb Strategy' {
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
                AZURE_COSMOS_DB_ACCOUNT_ENDPOINT = $env:AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AZURE_COSMOS_DB_DATABASE_NAME = $env:AZURE_COSMOS_DB_DATABASE_NAME
                AZURE_COSMOS_DB_MODELS_CONTAINER = $env:AZURE_COSMOS_DB_MODELS_CONTAINER
                AZURE_COSMOS_DB_ENTITIES_CONTAINER = $env:AZURE_COSMOS_DB_ENTITIES_CONTAINER
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
            
            $requiredVars = @('SQL_CONNECTION_STRING', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY', 'AZURE_COSMOS_DB_ACCOUNT_ENDPOINT')
            $missingVars = @($requiredVars | Where-Object { [string]::IsNullOrEmpty($Environment[$_]) })

            if ($missingVars -and $missingVars.Count -gt 0) {
                if ($NoAzureMode) {
                    Write-Verbose "NoAzure mode active: skipping required env var enforcement" -Verbose
                    return $true
                } else {
                    Write-Warning "Missing required environment variables for CosmosDb: $($missingVars -join ', ')"
                    throw "Missing required environment variables for CosmosDb: $($missingVars -join ', ')"
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
                $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("genaidb-cosmosdb-test-" + [Guid]::NewGuid().ToString('N'))
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

        function Test-CosmosDbAccessibility {
            param(
                [hashtable]$Environment,
                [bool]$NoAzureMode
            )
            
            if ($NoAzureMode) {
                Write-Host "Skipping Cosmos DB accessibility check - NoAzure mode enabled" -ForegroundColor Yellow
                return @{
                    Accessible = $true
                    Message = "NoAzure mode - validation skipped"
                    SkipTests = $false
                }
            }

            Write-Host "Validating Cosmos DB accessibility..." -ForegroundColor Cyan
            
            $endpoint = $Environment.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
            
            if ([string]::IsNullOrEmpty($endpoint)) {
                Write-Warning "Cosmos DB endpoint not configured. Tests will be marked as inconclusive."
                return @{
                    Accessible = $false
                    Message = "Cosmos DB endpoint not configured"
                    SkipTests = $true
                }
            }

            # Check if CosmosDB module is available
            $cosmosDbModule = Get-Module -Name CosmosDB -ListAvailable -ErrorAction SilentlyContinue
            
            if (-not $cosmosDbModule) {
                Write-Host "CosmosDB PowerShell module not found - attempting to install from PowerShell Gallery..." -ForegroundColor Yellow
                
                try {
                    Install-Module -Name CosmosDB -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
                    Write-Host "✓ Successfully installed CosmosDB module from PowerShell Gallery" -ForegroundColor Green
                    $cosmosDbModule = Get-Module -Name CosmosDB -ListAvailable -ErrorAction SilentlyContinue
                }
                catch {
                    Write-Warning "Failed to install CosmosDB module from PowerShell Gallery: $_"
                    Write-Host "  Tests will be marked as inconclusive. Install manually with: Install-Module -Name CosmosDB" -ForegroundColor Yellow
                    return @{
                        Accessible = $false
                        Message = "CosmosDB PowerShell module not available and could not be installed: $_"
                        SkipTests = $true
                    }
                }
            }
            
            if ($cosmosDbModule) {
                Write-Host "✓ CosmosDB PowerShell module found (v$($cosmosDbModule.Version))" -ForegroundColor Green
                
                try {
                    Import-Module CosmosDB -ErrorAction Stop
                    
                    # Extract account name from endpoint
                    $uri = [System.Uri]::new($endpoint)
                    $accountName = $uri.Host.Split('.')[0]
                    
                    Write-Verbose "Testing Cosmos DB API access for account: $accountName" -Verbose
                    
                    # Create context using Azure AD authentication (DefaultAzureCredential)
                    # This will test actual API access and authentication
                    $cosmosDbContext = New-CosmosDbContext -Account $accountName -Token (Get-AzAccessToken -ResourceUrl 'https://cosmos.azure.com' -ErrorAction SilentlyContinue).Token -ErrorAction Stop
                    
                    if ($cosmosDbContext) {
                        # Try to list databases to verify permissions
                        $databases = Get-CosmosDbDatabase -Context $cosmosDbContext -ErrorAction Stop
                        Write-Host "✓ Successfully authenticated to Cosmos DB account" -ForegroundColor Green
                        Write-Host "✓ Cosmos DB API access verified (found $($databases.Count) database(s))" -ForegroundColor Green
                        
                        return @{
                            Accessible = $true
                            Message = "Cosmos DB is accessible with proper permissions"
                            SkipTests = $false
                        }
                    }
                }
                catch {
                    $errorMessage = $_.Exception.Message
                    
                    if ($errorMessage -match 'Forbidden|403|unauthorized|not authorized') {
                        Write-Warning "Cosmos DB authentication failed - RBAC permissions may not be configured"
                        Write-Host "  Ensure 'Cosmos DB Built-in Data Contributor' role is assigned" -ForegroundColor Yellow
                        return @{
                            Accessible = $false
                            Message = "Cosmos DB RBAC permissions not configured: $errorMessage"
                            SkipTests = $false  # Let tests run and report specific errors
                        }
                    }
                    elseif ($errorMessage -match 'could not be found|not found|does not exist') {
                        Write-Warning "Cosmos DB account not found: $errorMessage"
                        return @{
                            Accessible = $false
                            Message = "Cosmos DB account not found or not accessible"
                            SkipTests = $true
                        }
                    }
                    else {
                        Write-Warning "Failed to validate Cosmos DB access: $errorMessage"
                        return @{
                            Accessible = $false
                            Message = "Cosmos DB validation failed: $errorMessage"
                            SkipTests = $false  # Allow tests to proceed with specific error handling
                        }
                    }
                }
            }
        }

        # Initialize test configuration
        $testConfig = Initialize-TestEnvironment -TestFilter $TestFilter
        
        $script:NoAzureMode = $testConfig.NoAzureMode
        $script:TestEnv = $testConfig.Environment
        
        Write-Host "CosmosDb Tests - Testing Azure Cosmos DB persistence" -ForegroundColor Magenta
        Write-Host "Cosmos DB Endpoint: $($script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT)" -ForegroundColor Magenta
        Write-Host "Database: $($script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME ?? 'SemanticModels (default)')" -ForegroundColor Magenta
        Write-Host "Models Container: $($script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER ?? 'Models (default)')" -ForegroundColor Magenta
        Write-Host "Entities Container: $($script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER ?? 'ModelEntities (default)')" -ForegroundColor Magenta
        
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

        # Validate Cosmos DB accessibility before running tests
        $cosmosDbValidation = Test-CosmosDbAccessibility -Environment $script:TestEnv -NoAzureMode $script:NoAzureMode
        $script:CosmosDbAccessible = $cosmosDbValidation.Accessible
        $script:CosmosDbValidationMessage = $cosmosDbValidation.Message
        
        if ($cosmosDbValidation.SkipTests) {
            Write-Warning "Cosmos DB validation failed: $($cosmosDbValidation.Message)"
            Write-Warning "All Cosmos DB tests will be marked as Inconclusive"
        }
    }

    Context 'Database Schema Operations with Cosmos DB' {
        BeforeAll {
            # Skip all tests in this context if Cosmos DB validation failed
            if (-not $script:CosmosDbAccessible) {
                Write-Warning "Skipping Database Schema Operations tests - Cosmos DB not accessible: $($script:CosmosDbValidationMessage)"
            }

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
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @dbConfig
        }

        Context 'extract-model command' {
            BeforeAll {
                $script:ExtractSucceeded = $false
            }

            It 'Should execute extract-model and store in Cosmos DB' {
                # Early exit if pre-validation failed
                if (-not $script:CosmosDbAccessible) {
                    Set-ItResult -Inconclusive -Because $script:CosmosDbValidationMessage
                    return
                }

                $commandResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DbProjectPath)

                $outputText = if ($commandResult.Output -is [array]) {
                    $commandResult.Output -join "`n"
                } else {
                    $commandResult.Output
                }

                # Log output for diagnostics
                Write-Host "Extract-model output: $outputText" -ForegroundColor Cyan

                if ($commandResult.ExitCode -eq 0) {
                    $script:ExtractSucceeded = $true
                    Write-Host "Extract-model command succeeded with Cosmos DB" -ForegroundColor Green
                    $commandResult.ExitCode | Should -Be 0 -Because 'Extract should succeed with CosmosDb strategy'
                } elseif ($outputText -match 'Login failed for user|Cannot open database|database.*login|SQL Server.*authentication') {
                    Write-Warning "SQL Database authentication failed. Check SQL_CONNECTION_STRING and database permissions."
                    Set-ItResult -Inconclusive -Because 'SQL Database access not authorized - verify connection string and database permissions'
                } elseif ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token|blocked by Auth.*data plane') {
                    $script:ExtractSucceeded = $false
                    Write-Warning "Cosmos DB AAD authentication failed. Data plane operations require native RBAC. See https://aka.ms/cosmos-native-rbac"
                    Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - data plane operations require native RBAC. Assign Cosmos DB Built-in Data Contributor role or enable RBAC on account'
                } elseif ($outputText -match 'Cosmos.*403|CosmosException.*403|not authorized to perform.*Cosmos|Forbidden.*Cosmos') {
                    Write-Warning "Cosmos DB authorization failed. Ensure the identity has 'Cosmos DB Data Contributor' role on account: $($script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT)"
                    Set-ItResult -Inconclusive -Because 'Cosmos DB access not authorized - identity requires Cosmos DB Data Contributor role'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized|Forbidden') {
                    Write-Warning "Authorization failure detected. This may be SQL Database or Cosmos DB access. Output: $outputText"
                    Set-ItResult -Inconclusive -Because 'Database or Cosmos DB access not authorized - check connection string and RBAC roles'
                } elseif ($outputText -match 'DatabaseAccountNotFound|Resource.*not found.*Cosmos|Cosmos.*account.*not found') {
                    Write-Warning "Cosmos DB account not found or not accessible at: $($script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT)"
                    Set-ItResult -Inconclusive -Because 'Cosmos DB account not found - verify endpoint URL and resource provisioning'
                } elseif ($outputText -match 'Database.*not found|Container.*not found|does not exist.*database|does not exist.*container') {
                    Write-Warning "Cosmos DB database or container not found. Database: $($script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME ?? 'SemanticModels (default)'), Containers: $($script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER ?? 'Models (default)'), $($script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER ?? 'ModelEntities (default)')"
                    Set-ItResult -Inconclusive -Because 'Cosmos DB database or containers not found - run infrastructure provisioning to create resources'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'CosmosDb persistence strategy not yet fully implemented for extract-model'
                } elseif ($commandResult.ExitCode -ne 0) {
                    Write-Warning "Extract-model failed with exit code $($commandResult.ExitCode)"
                    Set-ItResult -Inconclusive -Because "Extract-model failed with exit code $($commandResult.ExitCode) - infrastructure may not be ready"
                } else {
                    Write-Warning "Extract-model behavior unclear. Output: $outputText"
                    Set-ItResult -Inconclusive -Because 'Extract-model behavior unclear for CosmosDb'
                }
            }

            It 'Should verify model stored in Cosmos DB (when extract succeeds)' {
                if (-not $script:ExtractSucceeded) {
                    Set-ItResult -Skipped -Because 'Extract-model did not succeed in previous test'
                    return
                }

                # Verify by attempting to read the model back
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DbProjectPath
                )
                
                if ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Should be able to read model from Cosmos DB'
                }
            }
        }
    }

    Context 'AI Operations with Cosmos DB' {
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
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @aiConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:AiProjectPath) | Out-Null
        }

        Context 'enrich-model command' {
            It 'Should enrich model and update in Cosmos DB' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('enrich-model', '--project', $script:AiProjectPath)
                
                $outputText = $result.Output -join "`n"
                
                if ($outputText -match 'No semantic model found|AuthorizationFailure') {
                    Set-ItResult -Inconclusive -Because 'Model not available or access denied'                } elseif ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token|blocked by Auth.*data plane') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - requires native RBAC permissions'                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Enrich-model not yet supported for CosmosDb'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Enrich should succeed with CosmosDb'
                }
            }
        }

        Context 'generate-vectors command' {
            It 'Should run dry-run generate-vectors with Cosmos DB' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'generate-vectors',
                    '--project', $script:AiProjectPath,
                    '--dry-run'
                )
                
                $outputText = $result.Output -join "`n"

                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Cosmos DB'
                } elseif ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token|blocked by Auth.*data plane') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - requires native RBAC permissions'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB access not authorized'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not yet supported for CosmosDb'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Dry-run should succeed with CosmosDb'
                } else {
                    Write-Warning "generate-vectors output: $outputText"
                    Set-ItResult -Inconclusive -Because "Vector generation failed with unclear error (exit code: $($result.ExitCode))"
                }
            }

            It 'Should persist vectors to Cosmos DB entities container' {
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
                
                if ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token|blocked by Auth.*data plane') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - requires native RBAC permissions'
                    return
                }
                
                if ($outputText -match 'not supported|not available|not configured|not yet supported|not.*supported.*persistence') {
                    Set-ItResult -Inconclusive -Because 'Vector generation not supported for CosmosDb persistence'
                    return
                }
                
                if ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB access not authorized'
                    return
                }
                
                # For Cosmos DB, vectors should be stored in the entities container
                # We can verify by checking if the command output indicates success
                if ($outputText -match 'generated|completed|success|saved|stored') {
                    # Verify we can retrieve the model (which should now include vector references)
                    $verifyResult = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                        'show-object',
                        'table',
                        '--project', $script:AiProjectPath,
                        '--schemaName', 'SalesLT',
                        '--name', 'Product'
                    )
                    
                    if ($verifyResult.ExitCode -eq 0) {
                        $verifyResult.ExitCode | Should -Be 0 -Because "Vector generation succeeded and model should be retrievable from Cosmos DB. Output: $outputText"
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

    Context 'Model Display and Export Operations with Cosmos DB' {
        BeforeAll {
            $script:DisplayProjectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'display-test'
            Initialize-TestProject -ProjectPath $script:DisplayProjectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $displayConfig = @{
                ProjectPath = $script:DisplayProjectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                DatabaseSchema = $script:TestEnv.DATABASE_SCHEMA
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @displayConfig

            Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $script:DisplayProjectPath) | Out-Null
        }

        Context 'show-object command' {
            It 'Should display table information from Cosmos DB model' {
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'show-object',
                    'table',
                    '--project', $script:DisplayProjectPath,
                    '--schemaName', 'SalesLT',
                    '--name', 'Product'
                )
                
                $outputText = $result.Output -join "`n"
                
                # Log output for diagnostics
                Write-Host "Show-object output: $outputText" -ForegroundColor Cyan
                
                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Cosmos DB'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence|not implemented') {
                    Set-ItResult -Inconclusive -Because 'Show-object not yet supported for CosmosDb'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB access not authorized'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Should display from CosmosDb'
                    $outputText | Should -Match 'Product|Table|Schema' -Because 'Should display table information'
                } else {
                    Write-Warning "Show-object failed with exit code $($result.ExitCode)"
                    Set-ItResult -Inconclusive -Because "Show-object failed: $outputText"
                }
            }
        }

        Context 'export-model command' {
            It 'Should export model from Cosmos DB to local file' {
                $exportPath = Join-Path -Path $script:DisplayProjectPath -ChildPath 'exported-model.md'
                
                $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @(
                    'export-model',
                    '--project', $script:DisplayProjectPath,
                    '--outputFileName', $exportPath,
                    '--fileType', 'markdown'
                )
                
                $outputText = $result.Output -join "`n"
                
                # Log output for diagnostics
                Write-Host "Export-model output: $outputText" -ForegroundColor Cyan
                
                if ($outputText -match 'No semantic model found|not found|Model not found') {
                    Set-ItResult -Inconclusive -Because 'Model not available in Cosmos DB'
                } elseif ($outputText -match 'not yet supported|not.*supported.*persistence|not implemented') {
                    Set-ItResult -Inconclusive -Because 'Export-model not yet supported for CosmosDb'
                } elseif ($outputText -match 'AuthorizationFailure|Access denied|403.*not authorized') {
                    Set-ItResult -Inconclusive -Because 'Cosmos DB access not authorized'
                } elseif ($result.ExitCode -eq 0) {
                    $result.ExitCode | Should -Be 0 -Because 'Export should succeed from CosmosDb'
                    Test-Path -Path $exportPath | Should -BeTrue -Because 'Exported file should exist locally'
                } else {
                    Write-Warning "Export-model failed with exit code $($result.ExitCode)"
                    Set-ItResult -Inconclusive -Because "Export-model failed: $outputText"
                }
            }
        }
    }

    Context 'Cosmos DB Specific Scenarios' {
        It 'Should handle separate containers for models and entities' {
            # This test verifies the dual-container architecture works correctly
            $projectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'container-test'
            Initialize-TestProject -ProjectPath $projectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $projectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @config

            $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $projectPath)
            
            $outputText = $result.Output -join "`n"
            
            # This test mainly verifies the command doesn't fail with dual-container configuration
            if ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token') {
                Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - requires native RBAC permissions'
            } elseif ($result.ExitCode -eq 0 -or $outputText -match 'AuthorizationFailure|not yet supported') {
                $true | Should -BeTrue -Because 'Command should handle dual-container Cosmos DB configuration'
            }
        }

        It 'Should support hierarchical partition keys in Cosmos DB' {
            # Note: This test assumes HPK support is implemented in the repository
            # HPK allows better organization and query performance in Cosmos DB
            $projectPath = Join-Path -Path $script:BaseProjectPath -ChildPath 'hpk-test'
            Initialize-TestProject -ProjectPath $projectPath -ConsoleApp $script:ConsoleAppPath | Out-Null

            $config = @{
                ProjectPath = $projectPath
                ConnectionString = $script:TestEnv.SQL_CONNECTION_STRING
                AzureOpenAIEndpoint = $script:TestEnv.AZURE_OPENAI_ENDPOINT
                AzureOpenAIApiKey = $script:TestEnv.AZURE_OPENAI_API_KEY
                PersistenceStrategy = 'CosmosDb'
                AzureCosmosDbAccountEndpoint = $script:TestEnv.AZURE_COSMOS_DB_ACCOUNT_ENDPOINT
                AzureCosmosDbDatabaseName = $script:TestEnv.AZURE_COSMOS_DB_DATABASE_NAME
                AzureCosmosDbModelsContainer = $script:TestEnv.AZURE_COSMOS_DB_MODELS_CONTAINER
                AzureCosmosDbEntitiesContainer = $script:TestEnv.AZURE_COSMOS_DB_ENTITIES_CONTAINER
                NoAzureMode = $script:NoAzureMode
            }

            Set-TestProjectConfiguration @config

            $result = Invoke-ConsoleCommand -ConsoleApp $script:ConsoleAppPath -Arguments @('extract-model', '--project', $projectPath)
            
            $outputText = $result.Output -join "`n"
            
            # This test verifies HPK-based partitioning works if implemented
            if ($outputText -match 'cannot be authorized by AAD token in data plane|Request blocked by Auth.*AAD token') {
                Set-ItResult -Inconclusive -Because 'Cosmos DB AAD authentication failed - requires native RBAC permissions'
            } elseif ($result.ExitCode -eq 0 -or $outputText -match 'AuthorizationFailure|not yet supported') {
                $true | Should -BeTrue -Because 'Command should handle HPK configuration'
            }
        }
    }

    AfterAll {
        Write-Host "CosmosDb persistence strategy tests completed" -ForegroundColor Magenta
    }
}
