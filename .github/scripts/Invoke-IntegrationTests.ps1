param(
	[Parameter()]
	[string]
	$TestFilter
)

# Allow callers to specify a TestFilter (for example: 'no-azure') so the test process
# can detect no-azure mode without relying on external environment setup.
if ($TestFilter) {
	Write-Host "Setting TEST_FILTER environment variable to: $TestFilter"
	$env:TEST_FILTER = $TestFilter
}

# Run Pester integration tests with consistent configuration
New-Item -ItemType Directory -Path './test-results' -Force | Out-Null
$config = New-PesterConfiguration
$config.Run.Path = './src/Tests/Integration/Console.Integration.Tests.ps1'
$config.Output.Verbosity = 'Detailed'
$config.TestResult.Enabled = $true
$config.TestResult.OutputFormat = 'NUnitXml'
$config.TestResult.OutputPath = './test-results/integration-tests.xml'
$config.CodeCoverage.Enabled = $false
$config.Should.ErrorAction = 'Continue'

Write-Host "Running Pester with test script: $($config.Run.Path)"
Invoke-Pester -Configuration $config
