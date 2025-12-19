<#
    .SYNOPSIS
    Discovers and configures Azure Storage account for integration testing.

    .DESCRIPTION
    Finds the first Storage Account in the specified resource group, enables public access,
    configures network rules, and exports environment variables for test execution.

    .PARAMETER ResourceGroupName
    The Azure resource group containing the Storage Account.

    .PARAMETER Environment
    The environment identifier used for blob prefix generation.

    .PARAMETER RunId
    The unique run identifier used for blob prefix generation.

    .PARAMETER ContainerName
    The container name to use for storage operations. Defaults to 'semantic-models'.

    .EXAMPLE
    Set-AzureStorageForTests.ps1 -ResourceGroupName 'rg-test' -Environment 'dev' -RunId '12345'

    .OUTPUTS
    None. Exports AZURE_STORAGE_ACCOUNT_ENDPOINT, AZURE_STORAGE_CONTAINER, and AZURE_STORAGE_BLOB_PREFIX to $env:GITHUB_ENV.

    .NOTES
    This script is designed for CI/CD scenarios and requires Azure PowerShell modules.
#>
function Set-AzureStorageForTests {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Environment,
        
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$RunId,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$ContainerName = 'semantic-models'
    )

    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'

        Write-Verbose "Starting Azure Storage configuration for tests"
    }

    process {
        # Get runner IP for diagnostics
        function Get-RunnerIPAddress {
            [CmdletBinding()]
            param()
            
            try {
                $ip = (Invoke-RestMethod -Uri "https://ipinfo.io/ip" -Method Get).Trim()
                Write-Verbose "Retrieved runner IP address: $ip"
                return $ip
            }
            catch {
                Write-Warning "Could not retrieve runner IP address: $($_.Exception.Message)"
                return $null
            }
        }

        try {
            $runnerIP = Get-RunnerIPAddress
            if ($runnerIP) {
                Write-Host "GitHub Runner IP: $runnerIP"
            }
            
            # Find the first storage account in the resource group
            Write-Verbose "Searching for Storage Account in resource group: $ResourceGroupName"
            $storage = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName | Select-Object -First 1
            
            if (-not $storage) {
                Write-Warning "No Storage Account found in resource group $ResourceGroupName. AzureBlob persistence tests may be skipped/fail."
                return
            }
            
            Write-Host "Using Storage Account: $($storage.StorageAccountName)"

            # Ensure public access is enabled for the duration of tests
            Write-Verbose "Enabling public network access on Storage Account: $($storage.StorageAccountName)"
            Set-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $storage.StorageAccountName -PublicNetworkAccess Enabled -AllowBlobPublicAccess $true -Force | Out-Null

            # Attempt to set network rule default action to Allow (ignore if not supported)
            try {
                Write-Verbose "Updating storage network rule set to Allow"
                Update-AzStorageAccountNetworkRuleSet -ResourceGroupName $ResourceGroupName -Name $storage.StorageAccountName -DefaultAction Allow | Out-Null
            } catch {
                Write-Warning "Could not update storage network rule set: $($_.Exception.Message)"
            }

            # Create storage context for container operations
            $ctx = $storage.Context
            
            # Ensure the container exists
            Write-Verbose "Checking for container: $ContainerName"
            $container = Get-AzStorageContainer -Name $ContainerName -Context $ctx -ErrorAction SilentlyContinue
            
            if (-not $container) {
                Write-Host "Container '$ContainerName' does not exist. Creating it now..."
                try {
                    $container = New-AzStorageContainer -Name $ContainerName -Context $ctx -Permission Off
                    Write-Host "✅ Container '$ContainerName' created successfully"
                }
                catch {
                    Write-Error "Failed to create container '$ContainerName': $($_.Exception.Message)"
                    throw
                }
            }
            else {
                Write-Host "✅ Container '$ContainerName' already exists"
            }

            # Configure endpoints and export environment variables
            $endpoint = "https://$($storage.StorageAccountName).blob.core.windows.net"
            $blobPrefix = "$Environment/$RunId"
            
            Write-Host "Configured Storage endpoint: $endpoint"
            Write-Host "Container name: $ContainerName"
            Write-Host "Blob prefix: $blobPrefix"
            
            # Export environment variables for GitHub Actions using .NET configuration naming convention
            # .NET uses double underscores (__) for nested configuration sections
            "SemanticModelRepository__AzureBlob__AccountEndpoint=$endpoint" | Out-File -FilePath $env:GITHUB_ENV -Append
            "SemanticModelRepository__AzureBlob__ContainerName=$ContainerName" | Out-File -FilePath $env:GITHUB_ENV -Append
            "SemanticModelRepository__AzureBlob__BlobPrefix=$blobPrefix" | Out-File -FilePath $env:GITHUB_ENV -Append
            
            Write-Verbose "Successfully configured Azure Storage for tests"
        }
        catch {
            Write-Error "Storage discovery/configuration failed: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Azure Storage configuration for tests completed"
    }
}