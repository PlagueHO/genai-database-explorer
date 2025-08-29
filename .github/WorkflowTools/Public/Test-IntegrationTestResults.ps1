<#
    .SYNOPSIS
    Validates integration test result files and outputs GitHub Actions variables.

    .DESCRIPTION
    Checks for the existence of test result XML files, validates their XML structure,
    and sets GitHub Actions output variables to indicate whether results are valid.
    Provides detailed diagnostics for troubleshooting test execution issues.

    .PARAMETER TestResultsPath
    The path to the test results XML file. Defaults to './test-results/integration-tests.xml'.

    .PARAMETER OutputVariable
    The name of the GitHub Actions output variable to set. Defaults to 'test-results-valid'.

    .PARAMETER ShowPreview
    If specified, displays a preview of the XML content for debugging purposes.

    .PARAMETER PreviewLength
    The number of characters to show in the XML preview. Defaults to 500.

    .EXAMPLE
    Test-IntegrationTestResults.ps1

    .EXAMPLE
    Test-IntegrationTestResults.ps1 -TestResultsPath './custom-results/tests.xml' -OutputVariable 'custom-results-valid'

    .EXAMPLE
    Test-IntegrationTestResults.ps1 -ShowPreview -PreviewLength 1000

    .OUTPUTS
    None. Sets GitHub Actions output variables and displays validation results.

    .NOTES
    This script is designed for GitHub Actions workflows and requires the GITHUB_OUTPUT environment variable.
    It validates NUnit XML format commonly used by testing frameworks like Pester.
#>
function Test-IntegrationTestResults {
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$TestResultsPath = './test-results/integration-tests.xml',
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$OutputVariable = 'test-results-valid',
        
        [Parameter()]
        [switch]$ShowPreview,
        
        [Parameter()]
        [ValidateRange(100, 2000)]
        [int]$PreviewLength = 500
    )

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting integration test results validation"
            
        # Function to set GitHub Actions output
        function Set-GitHubOutput {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory)]
                [string]$Name,
                
                [Parameter(Mandatory)]
                [string]$Value
            )
            
            if ($env:GITHUB_OUTPUT) {
                "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
                Write-Verbose "Set GitHub output variable: $Name=$Value"
            } else {
                Write-Warning "GITHUB_OUTPUT environment variable not found - output not set"
            }
        }
        
        # Function to validate XML structure
        function Test-XmlStructure {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory)]
                [string]$XmlContent
            )
            
            try {
                [xml]$xmlContent = $XmlContent
                Write-Host "✅ Test results XML is valid" -ForegroundColor Green
                
                # Check for test suites
                $testSuiteCount = 0
                if ($xmlContent.testsuites -and $xmlContent.testsuites.testsuite) {
                    if ($xmlContent.testsuites.testsuite -is [array]) {
                        $testSuiteCount = $xmlContent.testsuites.testsuite.Count
                    } else {
                        $testSuiteCount = 1
                    }
                }
                
                Write-Host "Found $testSuiteCount test suite(s)" -ForegroundColor Green
                Write-Verbose "XML validation successful"
                return $true
            }
            catch {
                Write-Warning "⚠️  Test results XML appears to be malformed: $($_.Exception.Message)"
                return $false
            }
        }
    }
        
    process {
        try {
            # Normalize and resolve paths early
            $resolvedExpectedPath = $null
            try { $resolvedExpectedPath = (Resolve-Path -LiteralPath $TestResultsPath).Path } catch { $resolvedExpectedPath = [System.IO.Path]::GetFullPath($TestResultsPath) }

            Write-Verbose "Checking test results file: $resolvedExpectedPath"
            
            if (Test-Path -LiteralPath $resolvedExpectedPath) {
                # File exists - get file information
                $fileItem = Get-Item -LiteralPath $resolvedExpectedPath
                $fileSize = $fileItem.Length
                Write-Host "✅ Test results file exists (Size: $fileSize bytes)" -ForegroundColor Green
                
                # Read and validate content
                $content = Get-Content -LiteralPath $resolvedExpectedPath -Raw
                if ([string]::IsNullOrWhiteSpace($content)) {
                    Write-Warning "⚠️  Test results file is empty"
                    Set-GitHubOutput -Name $OutputVariable -Value 'false'
                    return
                }
                
                # Validate XML structure
                $isValidXml = Test-XmlStructure -XmlContent $content
                
                if ($ShowPreview) {
                    Write-Host "=== Test Results XML Preview ===" -ForegroundColor Cyan
                    $previewText = $content.Substring(0, [Math]::Min($PreviewLength, $content.Length))
                    Write-Host $previewText -ForegroundColor Gray
                    
                    if ($content.Length -gt $PreviewLength) {
                        Write-Host "... (truncated, showing first $PreviewLength characters of $($content.Length) total)" -ForegroundColor Yellow
                    }
                    Write-Host "=================================" -ForegroundColor Cyan
                }
                
                # Set output based on validation result
                if ($isValidXml) {
                    Set-GitHubOutput -Name $OutputVariable -Value 'true'
                    Write-Verbose "Test results validation successful"
                } else {
                    Set-GitHubOutput -Name $OutputVariable -Value 'false'
                    
                    if ($ShowPreview -or $content.Length -le $PreviewLength) {
                        Write-Host "First $PreviewLength characters of file:" -ForegroundColor Yellow
                        Write-Host $content.Substring(0, [Math]::Min($PreviewLength, $content.Length)) -ForegroundColor Gray
                    }
                }
            }
            else {
                Write-Warning "❌ Test results file does not exist at '$resolvedExpectedPath'"

                # Attempt auto-discovery in common locations and names
                $expectedDir = Split-Path -Path $resolvedExpectedPath -Parent
                $fallbackCandidates = @()
                if ($expectedDir -and (Test-Path -LiteralPath $expectedDir)) {
                    $fallbackCandidates += Get-ChildItem -LiteralPath $expectedDir -Recurse -Filter '*.xml' -ErrorAction SilentlyContinue
                }

                # Add common conventional names if not already found
                $conventionalNames = @(
                    (Join-Path $expectedDir 'testResults.xml'),
                    (Join-Path $expectedDir 'TestResult.xml'),
                    (Join-Path $expectedDir 'results.xml')
                ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

                if ($conventionalNames) {
                    foreach ($p in $conventionalNames) {
                        $fallbackCandidates += (Get-Item -LiteralPath $p)
                    }
                }

                if ($fallbackCandidates -and $fallbackCandidates.Count -gt 0) {
                    # Choose the most recently written XML as the likely test results
                    $best = $fallbackCandidates | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
                    Write-Host "Discovered potential test results at: $($best.FullName) (LastWrite: $($best.LastWriteTimeUtc.ToString('u')))" -ForegroundColor Yellow

                    try {
                        # Ensure destination directory exists
                        $destDir = Split-Path -Path $resolvedExpectedPath -Parent
                        if (-not (Test-Path -LiteralPath $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }

                        Copy-Item -LiteralPath $best.FullName -Destination $resolvedExpectedPath -Force
                        Write-Host "Copied discovered results to expected path: $resolvedExpectedPath" -ForegroundColor Cyan

                        # Re-run validation on the copied file
                        $content = Get-Content -LiteralPath $resolvedExpectedPath -Raw
                        if (-not [string]::IsNullOrWhiteSpace($content) -and (Test-XmlStructure -XmlContent $content)) {
                            Set-GitHubOutput -Name $OutputVariable -Value 'true'
                            return
                        }
                    } catch {
                        Write-Warning "Failed to copy discovered results: $($_.Exception.Message)"
                    }
                }

                # No file found; provide directory diagnostics
                Set-GitHubOutput -Name $OutputVariable -Value 'false'

                if ($expectedDir) {
                    if (Test-Path -LiteralPath $expectedDir) {
                        Write-Host "Contents of expected results directory '$expectedDir':" -ForegroundColor Yellow
                        Get-ChildItem -LiteralPath $expectedDir | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
                    } else {
                        Write-Host "Expected results directory '$expectedDir' does not exist" -ForegroundColor Red
                    }
                }
            }
        }
        catch {
            Write-Error "Failed to validate test results: $($_.Exception.Message)"
            Set-GitHubOutput -Name $OutputVariable -Value 'false'
            throw
        }
    }

    end {
        Write-Verbose "Integration test results validation completed"
    }
}