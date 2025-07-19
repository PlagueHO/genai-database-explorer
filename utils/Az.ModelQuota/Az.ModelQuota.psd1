@{
    # Module manifest for Az.ModelQuota
    RootModule = 'Az.ModelQuota.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    Author = 'GenAI Database Explorer'
    CompanyName = ''
    Copyright = '(c) 2025. All rights reserved.'
    Description = 'PowerShell module for retrieving Azure OpenAI model quota information'
    PowerShellVersion = '5.1'
    RequiredModules = @('Az.CognitiveServices', 'Az.Accounts')
    FunctionsToExport = @('Get-AzModelQuota')
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    PrivateData = @{
        PSData = @{
            Tags = @('Azure', 'OpenAI', 'Quota', 'CognitiveServices')
            LicenseUri = ''
            ProjectUri = ''
            ReleaseNotes = 'Initial release of Az.ModelQuota module'
        }
    }
}
