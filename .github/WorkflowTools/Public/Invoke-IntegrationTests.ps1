<#
    .SYNOPSIS
    Runs console integration tests with a consistent Pester configuration.

    .DESCRIPTION
    Creates a consistent Pester configuration for running integration tests.
    Optionally sets an environment variable `TEST_FILTER` to drive conditional test execution (e.g., no-azure scenarios).
    Fails with a non-zero exit code when tests fail.

    .PARAMETER TestFilter
    Optional filter string exported to environment variable `TEST_FILTER` for tests to consume.

    .PARAMETER TestResultsPath
    Path where test results XML file should be saved. Defaults to './test-results'.

    .PARAMETER TestScriptPath
    Path to the Pester test script to execute. Defaults to console integration tests.

    .EXAMPLE
    Invoke-IntegrationTests.ps1

    .EXAMPLE
    Invoke-IntegrationTests.ps1 -TestFilter 'no-azure'

    .OUTPUTS
    None. Creates test result files and exits with appropriate code.

    .NOTES
    This script requires Pester v5.7.1 or later and will install it if needed.
#>
function Invoke-IntegrationTests {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$TestFilter,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$TestResultsPath = './test-results',
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$TestScriptPath = './src/Tests/Integration/Console.Integration.Tests.ps1'
    )

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting integration tests execution"

        # Ensure Pester is available
        function Install-RequiredPester {
            [CmdletBinding()]
            param()
                    
            try {
                $minPesterVersion = [Version]'5.7.1'
                $pesterModule = Get-Module -ListAvailable -Name Pester | Sort-Object Version -Descending | Select-Object -First 1
                if (-not $pesterModule -or $pesterModule.Version -lt $minPesterVersion) {
                    Write-Host "Installing Pester (minimum $minPesterVersion)..." -ForegroundColor Yellow
                    Install-Module -Name Pester -Scope CurrentUser -Force -AllowClobber -MinimumVersion $minPesterVersion.ToString()
                }
                Import-Module Pester -MinimumVersion $minPesterVersion -Force -ErrorAction Stop | Out-Null
                Write-Verbose "Pester module successfully loaded"
            }
            catch {
                Write-Error "Failed to install/import Pester: $($_.Exception.Message)"
                throw
            }
        }
    }
        
    process {
        try {
            # Install and import Pester if needed
            Install-RequiredPester
            
            # Allow callers to specify a TestFilter (e.g., 'no-azure') to drive conditional tests
            if ($PSBoundParameters.ContainsKey('TestFilter') -and -not [string]::IsNullOrWhiteSpace($TestFilter)) {
                Write-Host "Setting TEST_FILTER environment variable to: $TestFilter"
                $env:TEST_FILTER = $TestFilter
            }

            # Create test results directory
            Write-Verbose "Creating test results directory: $TestResultsPath"
            New-Item -ItemType Directory -Path $TestResultsPath -Force | Out-Null
            
            # Configure Pester
            $config = New-PesterConfiguration
            $config.Run.Path = $TestScriptPath
            $config.Output.Verbosity = 'Detailed'
            $config.TestResult.Enabled = $true
            $config.TestResult.OutputFormat = 'NUnitXml'
            $config.TestResult.OutputPath = Join-Path $TestResultsPath 'integration-tests.xml'
            $config.CodeCoverage.Enabled = $false
            $config.Should.ErrorAction = 'Continue'
            
            # Set up script arguments using script parameters (Pester v5 approach)
            # Pass arguments directly via ScriptParameters hashtable if available
            $scriptParameters = @{}
            if ($env:PERSISTENCE_STRATEGY) { 
                $scriptParameters['PersistenceStrategy'] = $env:PERSISTENCE_STRATEGY 
            }
            if ($env:TEST_FILTER) { 
                $scriptParameters['TestFilter'] = $env:TEST_FILTER 
            }
            
            # Use Container approach for script parameters in Pester v5
            if ($scriptParameters.Count -gt 0) {
                $container = New-PesterContainer -Path $TestScriptPath -Data $scriptParameters
                $config.Run.Container = $container
            }

            Write-Host "Running Pester with test script: $($config.Run.Path)"
            Write-Verbose "Test results will be saved to: $($config.TestResult.OutputPath)"
            
            # Ensure Pester returns a result object in all environments (GitHub Actions included)
            # Some runners/modules may default to no return value unless passthrough is enabled.
            $config.Run.PassThru = $true
            $result = Invoke-Pester -Configuration $config -PassThru

            # Validate test results - Check for failures and provide detailed error info
            if ($null -ne $result) {
                # Try to access result properties safely, accommodating different Pester versions
                try {
                    $totalTests = 0
                    $failedTests = 0
                    $passedTests = 0
                    
                    # Some environments wrap the run result in a Result property
                    if ($result.PSObject.Properties.Name -contains 'Result' -and $null -ne $result.Result) {
                        $result = $result.Result
                    }

                    # Pester v5+ typically exposes these properties
                    if ($result.PSObject.Properties.Name -contains 'TotalCount') {
                        $totalTests = $result.TotalCount
                        $failedTests = $result.FailedCount
                        $passedTests = $result.PassedCount
                    }
                    # Fallback to Tests collection if main properties don't exist
                    elseif ($result.PSObject.Properties.Name -contains 'Tests') {
                        $totalTests = $result.Tests.Count
                        $failedTests = ($result.Tests | Where-Object { $_.Result -eq 'Failed' }).Count
                        $passedTests = ($result.Tests | Where-Object { $_.Result -eq 'Passed' }).Count
                    }
                    # Last resort - try to infer from result object properties
                    else {
                        Write-Warning "Unknown Pester result format. Available properties: $($result.PSObject.Properties.Name -join ', ')"
                        # Try common alternative property names
                        $totalTests = $result.Total ?? $result.TestCount ?? 0
                        $failedTests = $result.Failed ?? $result.FailureCount ?? 0
                        $passedTests = $result.Passed ?? $result.PassCount ?? 0
                    }
                    
                    Write-Host "Test execution completed." -ForegroundColor Cyan
                    Write-Host "Total tests: $totalTests, Passed: $passedTests, Failed: $failedTests" -ForegroundColor Cyan
                    
                    if ($failedTests -gt 0) {
                        Write-Error "Integration tests failed: $failedTests test(s) failed out of $totalTests."
                        exit 1
                    }
                    
                    Write-Host "Integration tests completed successfully." -ForegroundColor Green
                }
                catch {
                    Write-Warning "Error accessing Pester result properties: $($_.Exception.Message)"
                    Write-Host "Pester result object structure:" -ForegroundColor Yellow
                    $result | Format-List | Out-String | Write-Host
                    Write-Error "Integration tests failed: Unable to parse test results."
                    exit 1
                }
            } else {
                Write-Warning "Pester result object is null - this may indicate a configuration or execution error."
                Write-Error "Integration tests failed: No result object returned from Pester."
                exit 1
            }
        }
        catch {
            Write-Error "Failed to execute integration tests: $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Invoke-IntegrationTests process completed"
    }
}
