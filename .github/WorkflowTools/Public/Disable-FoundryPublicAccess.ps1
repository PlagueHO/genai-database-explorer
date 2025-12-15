<#
    .SYNOPSIS
    Disables public network access on a Microsoft Foundry (Cognitive Services) account.

    .DESCRIPTION
    Sets PublicNetworkAccess to Disabled and NetworkAcls.DefaultAction to Deny for the specified account, then verifies the change.

    .PARAMETER ResourceGroupName
    The Azure resource group containing the Microsoft Foundry account.

    .PARAMETER FoundryName
    The Microsoft Foundry (Cognitive Services) account name.

    .EXAMPLE
    Disable-FoundryPublicAccess.ps1 -ResourceGroupName 'rg-test' -FoundryName 'aif-123'

    .OUTPUTS
    None. Modifies network access settings on the specified Microsoft Foundry account.

    .NOTES
    Intended for CI usage. Uses Write-Host for progress messages and Write-Verbose for detailed output. Mutating operations are guarded by ShouldProcess.
#>
function Disable-FoundryPublicAccess {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$FoundryName
    )

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting Microsoft Foundry public access disablement"

        # Helper function to safely get property values with fallbacks
        function Get-AccountProperty {
            [CmdletBinding()]
            param($Account, [string]$PropertyPath, $DefaultValue = $null)
            
            $pathParts = $PropertyPath -split '\.'
            $current = $Account
            
            foreach ($part in $pathParts) {
                if ($current -and $current.PSObject.Properties.Name -contains $part) {
                    $current = $current.$part
                } elseif ($current -and $current.Properties -and $current.Properties.PSObject.Properties.Name -contains $part) {
                    $current = $current.Properties.$part
                } else {
                    return $DefaultValue
                }
            }
            return $current
        }

        # Helper function for retry operations
        function Invoke-WithRetry {
            [CmdletBinding()]
            param(
                [ScriptBlock]$ScriptBlock,
                [int]$MaxAttempts = 10,
                [string]$OperationName = "Operation"
            )
            
            for ($i = 1; $i -le $MaxAttempts; $i++) {
                try {
                    return & $ScriptBlock
                } catch {
                    $msg = $_.Exception.Message
                    if ($msg -match 'Conflict|Too Many Requests|temporar' -and $i -lt $MaxAttempts) {
                        $backoff = [Math]::Min(60, 3 * $i)
                        Write-Warning "$OperationName conflict/transient error: $msg. Retrying in $backoff s..."
                        Start-Sleep -Seconds $backoff
                        continue
                    }
                    throw
                }
            }
        }
    }

    process {
        try {
            Write-Host "Checking current network configuration for Microsoft Foundry service: $FoundryName"
            $account = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $FoundryName -ErrorAction Stop
            
            # Display current state using safe property access
            $currPublicNetworkAccess = Get-AccountProperty $account 'PublicNetworkAccess' '<not set>'
            $currDefaultAction = Get-AccountProperty $account 'NetworkAcls.DefaultAction' '<not set>'
            Write-Host "Current public network access: $currPublicNetworkAccess"
            Write-Host "Current network ACLs default action: $currDefaultAction"

            # Update network access with retry
            $target = "Microsoft Foundry '$FoundryName' in resource group '$ResourceGroupName'"
            Invoke-WithRetry -OperationName "Network access update" -ScriptBlock {
                if ($PSCmdlet.ShouldProcess($target, "Disable public network access and set DefaultAction=Deny")) {
                    # Disable public network access at the account level
                    Set-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $FoundryName -PublicNetworkAccess "Disabled" -Force -ErrorAction Stop

                    # Use the dedicated network rule cmdlet if available, otherwise fallback to account-level setting
                    if (Get-Command -Name Update-AzCognitiveServicesAccountNetworkRuleSet -ErrorAction SilentlyContinue) {
                        Update-AzCognitiveServicesAccountNetworkRuleSet -ResourceGroupName $ResourceGroupName -Name $FoundryName -DefaultAction Deny -ErrorAction Stop
                    } else {
                        Write-Verbose "Update-AzCognitiveServicesAccountNetworkRuleSet not available; using account-level NetworkRuleSet"
                        $networkRuleSet = @{ DefaultAction = 'Deny' }
                        Set-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $FoundryName -NetworkRuleSet $networkRuleSet -Force -ErrorAction Stop
                    }
                }
            }

            # Verify the changes
            $updatedAccount = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $FoundryName
            $updatedPublicNetworkAccess = Get-AccountProperty $updatedAccount 'PublicNetworkAccess' '<not set>'
            $updatedDefaultAction = Get-AccountProperty $updatedAccount 'NetworkAcls.DefaultAction' '<not set>'
            
            Write-Host "Updated public network access: $updatedPublicNetworkAccess"
            Write-Host "Updated network ACLs default action: $updatedDefaultAction"

            if ($updatedPublicNetworkAccess -eq "Disabled" -and $updatedDefaultAction -eq "Deny") {
                Write-Host "✅ Public network access successfully disabled for Microsoft Foundry service" -ForegroundColor Green
            } else {
                Write-Warning "⚠️ Network access may not be fully updated yet. Proceeding."
            }
        }
        catch {
            Write-Error "Failed to disable public access for Microsoft Foundry: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Microsoft Foundry public access disablement completed"
    }
}