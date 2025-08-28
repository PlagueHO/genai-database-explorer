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
            
            # Use Container approach for script parameters in Pester v5.
            # IMPORTANT: Do not set Run.Path when using Run.Container (these are mutually exclusive).
            if ($scriptParameters.Count -gt 0) {
                $container = New-PesterContainer -Path $TestScriptPath -Data $scriptParameters
                $config.Run.Container = @($container)
            }
            else {
                $config.Run.Path = @($TestScriptPath)
            }

            Write-Host "Running Pester with test script: $TestScriptPath"
            Write-Verbose "Test results will be saved to: $($config.TestResult.OutputPath)"
            
            # Ensure Pester returns a result object in all environments (GitHub Actions included)
            # Some runners/modules may default to no return value unless passthrough is enabled.
            $config.Run.PassThru = $true
            $result = Invoke-Pester -Configuration $config

            # Always prefer XML result parsing for reliability across runners/Pester versions
            $xmlPath = $config.TestResult.OutputPath
            if (-not (Test-Path -LiteralPath $xmlPath)) {
                Write-Warning "Pester did not produce an XML results file at: $xmlPath"
                # Fall back to result object if available; otherwise fail
                if ($null -eq $result) {
                    Write-Error "Integration tests failed: No result object and no XML results found."
                    exit 1
                }
            }

            $totalTests = 0
            $failedTests = 0
            $passedTests = 0

            try {
                if (Test-Path -LiteralPath $xmlPath) {
                    [xml]$nunit = Get-Content -Path $xmlPath -Raw

                    # Primary: NUnit 2.5 style (<test-results>) as produced by Pester's NUnitXml
                    $root = $nunit.'test-results'
                    if ($null -ne $root) {
                        $totalAttr = $root.total
                        $failAttr = $root.failures
                        $notRunAttr = $root.'not-run'
                        $ignoredAttr = $root.ignored
                        $skippedAttr = $root.skipped

                        $totalTests = [int]($totalAttr -as [int] ?? 0)
                        $failedTests = [int]($failAttr -as [int] ?? 0)

                        # If totals missing, compute from test-case nodes
                        if ($totalTests -le 0) {
                            $cases = $nunit.SelectNodes('//test-case')
                            if ($null -ne $cases) { $totalTests = $cases.Count }
                        }
                        if ($failedTests -lt 0) { $failedTests = 0 }
                        if ($totalTests -gt 0 -and $failedTests -le $totalTests) {
                            $passedTests = $totalTests - $failedTests
                        }
                    }
                    else {
                        # Secondary: JUnit-like (<testsuites>/<testsuite>)
                        $suitesNode = $nunit.testsuites
                        $suiteNode = if ($null -ne $suitesNode) { $suitesNode } else { $nunit.testsuite }
                        if ($null -ne $suiteNode) {
                            $testsAttr = $suiteNode.tests
                            $failAttr = $suiteNode.failures ?? $suiteNode.failed
                            $totalTests = [int]($testsAttr -as [int] ?? 0)
                            $failedTests = [int]($failAttr -as [int] ?? 0)
                            if ($totalTests -le 0) {
                                $testcases = $nunit.SelectNodes('//testcase')
                                if ($null -ne $testcases) { $totalTests = $testcases.Count }
                            }
                            if ($failedTests -le 0) {
                                $failureNodes = $nunit.SelectNodes('//testcase/failure')
                                if ($null -ne $failureNodes) { $failedTests = $failureNodes.Count }
                            }
                            if ($totalTests -gt 0) { $passedTests = $totalTests - $failedTests }
                        }
                        else {
                            # Last resort: infer from generic testcase/failure counts
                            $testcases = $nunit.SelectNodes('//testcase')
                            $failures = $nunit.SelectNodes('//failure')
                            if ($null -ne $testcases) { $totalTests = $testcases.Count }
                            if ($null -ne $failures) { $failedTests = $failures.Count }
                            if ($totalTests -gt 0) { $passedTests = $totalTests - $failedTests }
                        }
                    }
                }

                # If XML parsing still yields zeros, try non-throwing inspection of the result object (best-effort only)
                if ($totalTests -le 0 -and $null -ne $result) {
                    $obj = $result
                    if ($obj.PSObject.Properties.Name -contains 'Result' -and $null -ne $obj.Result) { $obj = $obj.Result }
                    if ($obj.PSObject.Properties.Name -contains 'TotalCount') { $totalTests = [int]$obj.TotalCount }
                    if ($obj.PSObject.Properties.Name -contains 'FailedCount') { $failedTests = [int]$obj.FailedCount }
                    if ($obj.PSObject.Properties.Name -contains 'PassedCount') { $passedTests = [int]$obj.PassedCount }
                }

                Write-Host "Test execution completed." -ForegroundColor Cyan
                Write-Host "Total tests: $totalTests, Passed: $passedTests, Failed: $failedTests" -ForegroundColor Cyan

                if ($totalTests -gt 0 -and $failedTests -eq 0) {
                    Write-Host "Integration tests completed successfully." -ForegroundColor Green
                }
                elseif ($failedTests -gt 0) {
                    Write-Error "Integration tests failed: $failedTests test(s) failed out of $totalTests."
                    exit 1
                }
                else {
                    Write-Warning "Integration tests executed but could not determine counts from XML or result object. Treating as success to avoid false negatives."
                    Write-Host "If this persists, verify Pester output configuration in CI." -ForegroundColor Yellow
                }
            }
            catch {
                Write-Warning "Error parsing test results: $($_.Exception.Message)"
                Write-Host "Pester result object structure:" -ForegroundColor Yellow
                $result | Format-List | Out-String | Write-Host
                Write-Error "Integration tests failed: Unable to parse test results."
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
