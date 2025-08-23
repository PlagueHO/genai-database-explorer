<#
.SYNOPSIS
Enables public network access on an Azure AI Foundry (Cognitive Services) account and waits until the resource is ready.

.DESCRIPTION
Resolves the AI Foundry account name from an explicit parameter or from a SQL server naming pattern, waits for the account to reach a terminal provisioning state with a valid endpoint,
then enables public network access with a permissive network rule set. Exports the account name and endpoint into the GitHub Actions environment for downstream steps.

.PARAMETER ResourceGroupName
The Azure resource group containing the AI Foundry account.

.PARAMETER AIFoundryName
Optional explicit AI Foundry account name. If omitted, the name is derived from the SQL server name pattern.

.PARAMETER SqlServerName
Optional SQL server name. When provided and following the pattern 'sql-<suffix>', the account name is assumed to be 'aif-<suffix>'.

.PARAMETER TimeoutSeconds
Maximum time to wait for the account to become ready.

.PARAMETER PollSeconds
Polling interval while waiting for the account.

.PARAMETER InitialJitterMinSeconds
Minimum seconds for initial random delay to reduce matrix contention.

.PARAMETER InitialJitterMaxSeconds
Maximum seconds for initial random delay to reduce matrix contention.

.EXAMPLE
./Enable-AIFoundryPublicAccess.ps1 -ResourceGroupName rg-demo -AIFoundryName aif-demo-123

.EXAMPLE
./Enable-AIFoundryPublicAccess.ps1 -ResourceGroupName rg-demo -SqlServerName sql-demo-123

.OUTPUTS
None. Exports environment variables AI_FOUNDRY_NAME and AZURE_OPENAI_ENDPOINT.

.NOTES
Intended for CI usage. Uses Write-Host for progress messages and Write-Verbose for detailed output. Mutating operations are guarded by ShouldProcess.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ResourceGroupName,

    [Parameter()]
    [string]$AIFoundryName,

    [Parameter()]
    [string]$SqlServerName,
    
    [Parameter()]
    [ValidateRange(60, 7200)]
    [int]$TimeoutSeconds = 1200,

    [Parameter()]
    [ValidateRange(1, 60)]
    [int]$PollSeconds = 10,

    [Parameter()]
    [ValidateRange(1, 60)]
    [int]$InitialJitterMinSeconds = 5,

    [Parameter()]
    [ValidateRange(1, 60)]
    [int]$InitialJitterMaxSeconds = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Verbose "Starting AI Foundry public access enablement"
        
        function Get-ProvisioningState {
            [CmdletBinding()]
            param($Account)
            try {
                if ($null -ne $Account.Properties -and $Account.Properties.PSObject.Properties.Name -contains 'ProvisioningState') {
                    return $Account.Properties.ProvisioningState
                }
                if ($Account.PSObject.Properties.Name -contains 'ProvisioningState') {
                    return $Account.ProvisioningState
                }
            } catch { }
            return $null
        }

        function Wait-ForFoundryReady {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory)] [string] $ResourceGroup,
                [Parameter(Mandatory)] [string] $Name,
                [Parameter()] [int] $TimeoutSeconds = 1200,
                [Parameter()] [int] $PollSeconds = 10
            )
            $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
            while ([DateTime]::UtcNow -lt $deadline) {
                try {
                    $acc = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroup -Name $Name -ErrorAction Stop
                    $state = Get-ProvisioningState $acc
                    $endpoint = $acc.Properties.Endpoint
                    if ([string]::IsNullOrEmpty($endpoint) -and $acc.PSObject.Properties.Name -contains 'Endpoint') { $endpoint = $acc.Endpoint }
                    Write-Verbose "Provisioning state: '$state', Endpoint: '$endpoint'"
                    if ($state -in @('Succeeded','SucceededWithWarnings','Active') -and -not [string]::IsNullOrEmpty($endpoint)) {
                        return $acc
                    }
                } catch {
                    Write-Verbose "Waiting for Foundry account to be discoverable... $_"
                }
                Start-Sleep -Seconds $PollSeconds
            }
            throw "AI Foundry service '$Name' was not ready within timeout ($TimeoutSeconds s)."
        }
        
        # Initial jitter to reduce matrix contention
        $initialDelay = Get-Random -Minimum $InitialJitterMinSeconds -Maximum $InitialJitterMaxSeconds
        Write-Host "Initial jitter delay: $initialDelay seconds"
        Start-Sleep -Seconds $initialDelay
    }
    
    process {
        try {
            # Determine Foundry name
            $resolvedFoundryName = $null
            if (-not [string]::IsNullOrEmpty($AIFoundryName)) {
                $resolvedFoundryName = $AIFoundryName
                Write-Host "Using provided AI Foundry service name: $resolvedFoundryName"
            } elseif ($SqlServerName -match '^sql-(.+)$') {
                $envSuffix = $matches[1]
                $resolvedFoundryName = "aif-$envSuffix"
                Write-Host "Constructed AI Foundry service name from SQL server: $resolvedFoundryName"
            } else {
                Write-Warning "Could not determine AI Foundry service name (no input and SQL name not matching pattern). Skipping public access configuration."
                return
            }

            Write-Host "Waiting for AI Foundry service '$resolvedFoundryName' to be ready..."
            $account = Wait-ForFoundryReady -ResourceGroup $ResourceGroupName -Name $resolvedFoundryName -TimeoutSeconds $TimeoutSeconds -PollSeconds $PollSeconds

            Write-Host "Current public network access: $($account.Properties.PublicNetworkAccess)"
            Write-Host "Current network ACLs default action: $($account.Properties.NetworkAcls.DefaultAction)"

            $networkRuleSet = New-Object Microsoft.Azure.Commands.Management.CognitiveServices.Models.PSNetworkRuleSet
            $networkRuleSet.DefaultAction = "Allow"

            $target = "AI Foundry '$resolvedFoundryName' in resource group '$ResourceGroupName'"
            $maxAttempts = 10
            for ($i=1; $i -le $maxAttempts; $i++) {
                try {
                    if ($PSCmdlet.ShouldProcess($target, "Enable public network access and set DefaultAction=Allow")) {
                        Set-AzCognitiveServicesAccount `
                            -ResourceGroupName $ResourceGroupName `
                            -Name $resolvedFoundryName `
                            -PublicNetworkAccess "Enabled" `
                            -NetworkRuleSet $networkRuleSet `
                            -Force
                    }
                    break
                } catch {
                    $msg = $_.Exception.Message
                    if ($msg -match 'Conflict' -or $msg -match 'Too Many Requests' -or $msg -match 'temporar') {
                        $backoff = [Math]::Min(60, 3 * $i)
                        Write-Warning "Update conflict/transient error: $msg. Retrying in $backoff s..."
                        Start-Sleep -Seconds $backoff
                        continue
                    }
                    throw
                }
            }

            $updatedAccount = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName
            Write-Host "Updated public network access: $($updatedAccount.Properties.PublicNetworkAccess)"
            Write-Host "Updated network ACLs default action: $($updatedAccount.Properties.NetworkAcls.DefaultAction)"

            if ($updatedAccount.Properties.PublicNetworkAccess -eq "Enabled" -and $updatedAccount.Properties.NetworkAcls.DefaultAction -eq "Allow") {
                Write-Host "✅ Public network access enabled for AI Foundry service" -ForegroundColor Green
            } else {
                Write-Warning "⚠️ Network access may not be fully updated yet. Proceeding."
            }

            # Export name and endpoint for later steps
            "AI_FOUNDRY_NAME=$resolvedFoundryName" | Out-File -FilePath $env:GITHUB_ENV -Append
            $endpoint = $updatedAccount.Properties.Endpoint
            if ([string]::IsNullOrEmpty($endpoint) -and $updatedAccount.PSObject.Properties.Name -contains 'Endpoint') { $endpoint = $updatedAccount.Endpoint }
            if (-not [string]::IsNullOrEmpty($endpoint)) {
                "AZURE_OPENAI_ENDPOINT=$endpoint" | Out-File -FilePath $env:GITHUB_ENV -Append
                Write-Host "Exported endpoint: $endpoint"
            }
        }
    }
}
catch {
    Write-Error "Failed to enable public access for AI Foundry: $($_.Exception.Message)"
    throw
}

Write-Verbose "AI Foundry public access enablement completed"
