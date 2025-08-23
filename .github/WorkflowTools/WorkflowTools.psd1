@{
    # Module manifest for WorkflowTools module
    RootModule = 'WorkflowTools.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'd4c3b2a1-0f9e-8765-4321-fedcba098765'
    Author = 'GenAI Database Explorer'
    CompanyName = 'GenAI Database Explorer'
    Copyright = '(c) 2025 GenAI Database Explorer. All rights reserved.'
    Description = 'PowerShell module with helper functions used by GitHub Actions workflows'

    # PowerShell version requirements
    PowerShellVersion = '7.0'

    # Required modules for this manifest
    RequiredModules = @(
        'Az.Accounts',
        'Az.Sql',
        'Az.CognitiveServices',
        'Az.Storage',
        'Az.CosmosDB'
    )

    # Functions to export from this module
    FunctionsToExport = @(
        'Get-GitHubRunnerOS',
        'Set-WorkflowOutput',
        'Write-WorkflowLog',
        'Get-RepoInfo',
        'Add-SqlServerFirewallRule',
        'Remove-SqlServerFirewallRule',
        'Disable-AIFoundryPublicAccess',
        'Enable-AIFoundryPublicAccess',
        'Get-AIFoundryApiKey',
        'Invoke-IntegrationTests',
        'Set-AzureStorageForTests',
        'Set-CosmosDbForTests',
        'Test-IntegrationTestResults'
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
            Tags = @('Azure', 'Workflow', 'GitHub', 'CI')

            # A URL to the license for this module
            LicenseUri = ''

            # A URL to the main website for this project
            ProjectUri = ''

            # A URL to an icon representing this module
            IconUri = ''

            # Release notes for this module
            ReleaseNotes = @'
'@
        }
    }
}
