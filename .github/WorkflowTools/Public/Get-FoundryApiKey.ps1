<#
    .SYNOPSIS
    Retrieves the Azure OpenAI API key from a Microsoft Foundry (Cognitive Services) account and exports it to the GitHub Actions environment.

    .DESCRIPTION
    Resolves the target account name and attempts multiple times to retrieve the account keys, writing `AZURE_OPENAI_API_KEY` into `$GITHUB_ENV` when successful.

    .PARAMETER ResourceGroupName
    The resource group containing the Microsoft Foundry account.

    .PARAMETER FoundryName
    Optional explicit account name. If omitted, derives from the environment or SQL server naming pattern.

    .PARAMETER SqlServerName
    Optional SQL server name to derive the account name (pattern: sql-<suffix> => aif-<suffix>).

    .PARAMETER MaxAttempts
    Maximum retries when fetching the key.

    .EXAMPLE
    Get-FoundryApiKey.ps1 -ResourceGroupName 'rg-test' -FoundryName 'aif-123'

    .EXAMPLE
    Get-FoundryApiKey.ps1 -ResourceGroupName 'rg-test' -SqlServerName 'sql-123'

    .OUTPUTS
    None. Exports AZURE_OPENAI_API_KEY to $env:GITHUB_ENV.

    .NOTES
    This script retries with exponential backoff when key retrieval fails.
#>
function Get-FoundryApiKey {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceGroupName,
        
        [Parameter()]
        [string]$FoundryName,
        
        [Parameter()]
        [string]$SqlServerName,
        
        [Parameter()]
        [ValidateRange(1, 100)]
        [int]$MaxAttempts = 10
    )

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting Microsoft Foundry API key retrieval"
    }

    process {
        try {
            # Resolve Microsoft Foundry name
            $resolvedFoundryName = $null
            if (-not [string]::IsNullOrEmpty($FoundryName)) {
                $resolvedFoundryName = $FoundryName
                Write-Host "Using provided Microsoft Foundry service name: $resolvedFoundryName"
            } elseif (-not [string]::IsNullOrEmpty($env:AI_FOUNDRY_NAME)) {
                $resolvedFoundryName = $env:AI_FOUNDRY_NAME
                Write-Host "Using Microsoft Foundry service name from environment: $resolvedFoundryName"
            } elseif ($SqlServerName -match '^sql-(.+)$') {
                $resolvedFoundryName = "aif-$($matches[1])"
                Write-Host "Constructed Microsoft Foundry service name from SQL server pattern: $resolvedFoundryName"
            } else {
                Write-Warning "Could not resolve Microsoft Foundry service name; skipping API key retrieval."
                return
            }

            # Retrieve API key with retry logic
            for ($i = 1; $i -le $MaxAttempts; $i++) {
                try {
                    Write-Verbose "Attempt $i of $MaxAttempts to retrieve API key for '$resolvedFoundryName'"
                    
                    $account = Get-AzCognitiveServicesAccount -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName -ErrorAction Stop
                    $keys = Get-AzCognitiveServicesAccountKey -ResourceGroupName $ResourceGroupName -Name $resolvedFoundryName -ErrorAction Stop
                    
                    if ($keys.Key1) {
                        "AZURE_OPENAI_API_KEY=$($keys.Key1)" | Out-File -FilePath $env:GITHUB_ENV -Append
                        Write-Host "✅ Retrieved Azure OpenAI API key" -ForegroundColor Green
                        return
                    }
                    
                    throw "Keys not available yet"
                } catch {
                    if ($i -eq $MaxAttempts) { 
                        Write-Warning "Failed to retrieve API key after $MaxAttempts attempts: $($_.Exception.Message)"
                        throw
                    }
                    
                    $backoff = [Math]::Min(60, 3 * $i)
                    Write-Host "Attempt $i failed: $($_.Exception.Message). Retrying in $backoff s..." -ForegroundColor Yellow
                    Start-Sleep -Seconds $backoff
                }
            }
        }
        catch {
            Write-Error "Failed to retrieve API key for Microsoft Foundry: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Microsoft Foundry API key retrieval completed"
    }
}