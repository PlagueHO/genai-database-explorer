@{
    # Module manifest for TestHelper module
    RootModule = 'TestHelper.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a1b2c3d4-e5f6-7890-1234-567890abcdef'
    Author = 'GenAI Database Explorer Team'
    CompanyName = 'GenAI Database Explorer'
    Copyright = '(c) 2025 GenAI Database Explorer Team. All rights reserved.'
    Description = 'Test Helper module providing fixture support functions for GenAI Database Explorer Console integration tests'

    # PowerShell version requirements
    PowerShellVersion = '7.0'

    # Functions to export from this module
    FunctionsToExport = @(
        'Initialize-TestProject',
        'Set-ProjectSettings',
        'Invoke-ConsoleCommand',
        'New-TestDataDictionary',
        'Set-TestProjectConfiguration'
    )

    # Cmdlets to export from this module
    CmdletsToExport = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess
    PrivateData = @{
        PSData = @{
            # Tags applied to this module
            Tags = @('Testing', 'Integration', 'GenAI', 'Database', 'Console')

            # A URL to the license for this module
            LicenseUri = ''

            # A URL to the main website for this project
            ProjectUri = ''

            # A URL to an icon representing this module
            IconUri = ''

            # Release notes for this module
            ReleaseNotes = @'
## 1.0.1
- Added Set-TestProjectConfiguration function for unified test project setup (database and Azure OpenAI settings)

## 1.0.0
- Initial release
- Added Initialize-TestProject function for project setup
- Added Set-ProjectSettings function for configuration management
- Added Invoke-ConsoleCommand function for standardized command execution
- Added New-TestDataDictionary function for test data creation
'@
        }
    }
}
