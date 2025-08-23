function Disable-AIFoundryPublicAccess {
    <#
    .SYNOPSIS
    Disables public network access on an Azure AI Foundry (Cognitive Services) account.

    .DESCRIPTION
    Sets PublicNetworkAccess to Disabled and NetworkAcls.DefaultAction to Deny for the specified account, then verifies the change.

    .PARAMETER ResourceGroupName
    The Azure resource group containing the AI Foundry account.

    .PARAMETER AIFoundryName
    The AI Foundry (Cognitive Services) account name.

    .EXAMPLE
    Disable-AIFoundryPublicAccess -ResourceGroupName 'rg-test' -AIFoundryName 'aif-123'
    
    .OUTPUTS
    None. Modifies network access settings on the specified AI Foundry account.
    
    .NOTES
    This function supports -WhatIf and requires confirmation for security changes.
    #>
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$AIFoundryName
    )
    
    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'
        
        Write-Verbose "Starting Disable-AIFoundryPublicAccess process"
    }
    
    process {
        try {
            Write-Host "Checking current network configuration for AI Foundry service: $AIFoundryName"
            $account = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $AIFoundryName -ErrorAction Stop
            Write-Host "Current public network access: $($account.Properties.PublicNetworkAccess)"
            Write-Host "Current network ACLs default action: $($account.Properties.NetworkAcls.DefaultAction)"

            Write-Host "Disabling public network access for AI Foundry service: $AIFoundryName"
            $networkRuleSet = New-Object Microsoft.Azure.Commands.Management.CognitiveServices.Models.PSNetworkRuleSet
            $networkRuleSet.DefaultAction = "Deny"

            $target = "AI Foundry '$AIFoundryName' in resource group '$ResourceGroupName'"
            if ($PSCmdlet.ShouldProcess($target, "Disable public network access and set DefaultAction=Deny")) {
                Set-AzCognitiveServicesAccount `
                    -ResourceGroupName $ResourceGroupName `
                    -Name $AIFoundryName `
                    -PublicNetworkAccess "Disabled" `
                    -NetworkRuleSet $networkRuleSet `
                    -Force
                
                Write-Verbose "Successfully updated network access settings"
            }

            $updatedAccount = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $AIFoundryName
            Write-Host "Updated public network access: $($updatedAccount.Properties.PublicNetworkAccess)"
            Write-Host "Updated network ACLs default action: $($updatedAccount.Properties.NetworkAcls.DefaultAction)"

            if ($updatedAccount.Properties.PublicNetworkAccess -eq "Disabled" -and $updatedAccount.Properties.NetworkAcls.DefaultAction -eq "Deny") {
                Write-Host "✅ Public network access successfully disabled for AI Foundry service" -ForegroundColor Green
            } else {
                Write-Warning "⚠️  Network access may not have been properly updated."
            }
        }
        catch [Microsoft.Azure.Commands.Management.CognitiveServices.Models.CognitiveServicesException] {
            Write-Warning "Could not find AI Foundry service: $AIFoundryName"
            Write-Error "Cognitive Services error: $($_.Exception.Message)"
            throw
        }
        catch {
            Write-Error "Failed to disable public access for AI Foundry: $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Disable-AIFoundryPublicAccess process completed"
    }
}

# Call the function with script parameters when run as script
if ($MyInvocation.InvocationName -ne '.') {
    Disable-AIFoundryPublicAccess @PSBoundParameters
}
