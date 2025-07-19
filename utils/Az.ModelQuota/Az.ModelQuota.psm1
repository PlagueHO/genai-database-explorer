#Requires -Modules Az.CognitiveServices, Az.Accounts

<#
.SYNOPSIS
    Az.ModelQuota PowerShell Module

.DESCRIPTION
    This module provides functions for retrieving Azure OpenAI model quota information.

.NOTES
    This module requires Az.CognitiveServices and Az.Accounts PowerShell modules.
#>

# Import the function
. $PSScriptRoot\Public\Get-AzModelQuota.ps1

# Export the function
Export-ModuleMember -Function Get-AzModelQuota
