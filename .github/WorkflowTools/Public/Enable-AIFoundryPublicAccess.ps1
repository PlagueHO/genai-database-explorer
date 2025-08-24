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
    Enable-AIFoundryPublicAccess.ps1 -ResourceGroupName rg-demo -AIFoundryName aif-demo-123

    .EXAMPLE
    Enable-AIFoundryPublicAccess.ps1 -ResourceGroupName rg-demo -SqlServerName sql-demo-123

    .OUTPUTS
    None. Exports environment variables AI_FOUNDRY_NAME and AZURE_OPENAI_ENDPOINT.

    .NOTES
    Intended for CI usage. Uses Write-Host for progress messages and Write-Verbose for detailed output. Mutating operations are guarded by ShouldProcess.
#>
function Enable-AIFoundryPublicAccess {
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

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting AI Foundry public access enablement"

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
                    $state = Get-AccountProperty $acc 'ProvisioningState'
                    $endpoint = Get-AccountProperty $acc 'Endpoint'
                    
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
            $resolvedFoundryName = if (-not [string]::IsNullOrEmpty($AIFoundryName)) {
                Write-Host "Using provided AI Foundry service name: $AIFoundryName"
                $AIFoundryName
            } elseif ($SqlServerName -match '^sql-(.+)$') {
                $envSuffix = $matches[1]
                $computedName = "aif-$envSuffix"
                Write-Host "Constructed AI Foundry service name from SQL server: $computedName"
                $computedName
            } else {
                Write-Warning "Could not determine AI Foundry service name (no input and SQL name not matching pattern). Skipping public access configuration."
                return
            }

            Write-Host "Waiting for AI Foundry service '$resolvedFoundryName' to be ready..."
            $account = Wait-ForFoundryReady -ResourceGroup $ResourceGroupName -Name $resolvedFoundryName -TimeoutSeconds $TimeoutSeconds -PollSeconds $PollSeconds

            # Display current state
            $currPublicNetworkAccess = Get-AccountProperty $account 'PublicNetworkAccess' '<not set>'
            $currDefaultAction = Get-AccountProperty $account 'NetworkAcls.DefaultAction' '<not set>'
            Write-Host "Current public network access: $currPublicNetworkAccess"
            Write-Host "Current network ACLs default action: $currDefaultAction"

            # Update network access with retry
            $target = "AI Foundry '$resolvedFoundryName' in resource group '$ResourceGroupName'"
            Invoke-WithRetry -OperationName "Network access update" -ScriptBlock {
                if ($PSCmdlet.ShouldProcess($target, "Enable public network access and set DefaultAction=Allow")) {
                    # Enable public network access at the account level
                    Set-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName -PublicNetworkAccess "Enabled" -Force -ErrorAction Stop

                    # Use the dedicated network rule cmdlet if available, otherwise fallback to account-level setting
                    if (Get-Command -Name Update-AzCognitiveServicesAccountNetworkRuleSet -ErrorAction SilentlyContinue) {
                        Update-AzCognitiveServicesAccountNetworkRuleSet -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName -DefaultAction Allow -ErrorAction Stop
                    } else {
                        Write-Verbose "Update-AzCognitiveServicesAccountNetworkRuleSet not available; using account-level NetworkRuleSet"
                        $networkRuleSet = @{ DefaultAction = 'Allow' }
                        Set-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName -NetworkRuleSet $networkRuleSet -Force -ErrorAction Stop
                    }
                }
            }

            # Verify the changes
            $updatedAccount = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName
            $updatedPublicNetworkAccess = Get-AccountProperty $updatedAccount 'PublicNetworkAccess' '<not set>'
            $updatedDefaultAction = Get-AccountProperty $updatedAccount 'NetworkAcls.DefaultAction' '<not set>'
            
            Write-Host "Updated public network access: $updatedPublicNetworkAccess"
            Write-Host "Updated network ACLs default action: $updatedDefaultAction"

            if ($updatedPublicNetworkAccess -eq "Enabled" -and $updatedDefaultAction -eq "Allow") {
                Write-Host "✅ Public network access enabled for AI Foundry service" -ForegroundColor Green
            } else {
                Write-Warning "⚠️ Network access may not be fully updated yet. Proceeding."
            }

            # Export environment variables for downstream steps
            "AI_FOUNDRY_NAME=$resolvedFoundryName" | Out-File -FilePath $env:GITHUB_ENV -Append
            
            $endpoint = Get-AccountProperty $updatedAccount 'Endpoint'
            if (-not [string]::IsNullOrEmpty($endpoint)) {
                "AZURE_OPENAI_ENDPOINT=$endpoint" | Out-File -FilePath $env:GITHUB_ENV -Append
                Write-Host "Exported endpoint: $endpoint"
            }
        }
        catch {
            Write-Error "Failed to enable public access for AI Foundry: $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "AI Foundry public access enablement completed"
    }
}