function Set-CosmosDbForTests {
    <#
    .SYNOPSIS
    Discovers and configures Azure Cosmos DB account for integration testing.

    .DESCRIPTION
    Finds the first Cosmos DB Account in the specified resource group, enables public network access,
    and exports environment variables for test execution including endpoint and container configurations.

    .PARAMETER ResourceGroupName
    The Azure resource group containing the Cosmos DB Account.

    .PARAMETER DatabaseName
    The database name to use for Cosmos DB operations. Defaults to 'SemanticModels'.

    .PARAMETER ModelsContainer
    The container name for storing models. Defaults to 'Models'.

    .PARAMETER EntitiesContainer
    The container name for storing entities. Defaults to 'ModelEntities'.

    .EXAMPLE
    Set-CosmosDbForTests -ResourceGroupName 'rg-test'

    .EXAMPLE
    Set-CosmosDbForTests -ResourceGroupName 'rg-test' -DatabaseName 'TestDB' -ModelsContainer 'TestModels'

    .OUTPUTS
    None. Exports AZURE_COSMOS_DB_ACCOUNT_ENDPOINT, AZURE_COSMOS_DB_DATABASE_NAME, AZURE_COSMOS_DB_MODELS_CONTAINER, and AZURE_COSMOS_DB_ENTITIES_CONTAINER to $env:GITHUB_ENV.

    .NOTES
    This function is designed for CI/CD scenarios and requires Azure PowerShell modules.
    Supports multiple Cosmos DB PowerShell cmdlet variants across different Az module versions.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceGroupName,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$DatabaseName = 'SemanticModels',
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$ModelsContainer = 'Models',
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$EntitiesContainer = 'ModelEntities'
    )
    
    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'
        
        Write-Verbose "Starting Set-CosmosDbForTests process"
        
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
        
        # Enable public network access using available cmdlets
        function Enable-CosmosDbPublicAccess {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory)]
                [string]$ResourceGroupName,
                
                [Parameter(Mandatory)]
                [string]$AccountName
            )
            
            try {
                # Update cmdlet name varies across Az versions; attempt common variants
                if (Get-Command Update-AzCosmosDBAccount -ErrorAction SilentlyContinue) {
                    Write-Verbose "Using Update-AzCosmosDBAccount cmdlet"
                    Update-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $AccountName -PublicNetworkAccess Enabled | Out-Null
                } elseif (Get-Command Set-AzCosmosDBAccount -ErrorAction SilentlyContinue) {
                    Write-Verbose "Using Set-AzCosmosDBAccount cmdlet"
                    Set-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $AccountName -PublicNetworkAccess Enabled | Out-Null
                } else {
                    Write-Warning "No suitable Cosmos DB cmdlet found for enabling public access"
                }
            } catch {
                Write-Warning "Could not enable public network access on Cosmos DB: $($_.Exception.Message)"
            }
        }
    }
    
    process {
        try {
            $runnerIP = Get-RunnerIPAddress
            if ($runnerIP) {
                Write-Host "GitHub Runner IP: $runnerIP"
            }
            
            # Find the first Cosmos DB account in the resource group
            Write-Verbose "Searching for Cosmos DB Account in resource group: $ResourceGroupName"
            $cosmosDb = Get-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName | Select-Object -First 1
            
            if (-not $cosmosDb) {
                Write-Warning "No Cosmos DB Account found in resource group $ResourceGroupName. Cosmos DB persistence tests may be skipped/fail."
                return
            }
            
            Write-Host "Using Cosmos DB Account: $($cosmosDb.Name)"

            # Enable public network access for tests
            Write-Verbose "Enabling public network access on Cosmos DB Account: $($cosmosDb.Name)"
            Enable-CosmosDbPublicAccess -ResourceGroupName $ResourceGroupName -AccountName $cosmosDb.Name

            # Compose endpoint and default database/containers matching app defaults
            $endpoint = $cosmosDb.DocumentEndpoint
            if (-not $endpoint) { 
                $endpoint = "https://$($cosmosDb.Name).documents.azure.com:443/" 
                Write-Verbose "Constructed endpoint from account name: $endpoint"
            }
            
            Write-Host "Configured Cosmos DB endpoint: $endpoint"
            Write-Host "Database name: $DatabaseName"
            Write-Host "Models container: $ModelsContainer"
            Write-Host "Entities container: $EntitiesContainer"
            
            # Export environment variables for GitHub Actions
            "AZURE_COSMOS_DB_ACCOUNT_ENDPOINT=$endpoint" | Out-File -FilePath $env:GITHUB_ENV -Append
            "AZURE_COSMOS_DB_DATABASE_NAME=$DatabaseName" | Out-File -FilePath $env:GITHUB_ENV -Append
            "AZURE_COSMOS_DB_MODELS_CONTAINER=$ModelsContainer" | Out-File -FilePath $env:GITHUB_ENV -Append
            "AZURE_COSMOS_DB_ENTITIES_CONTAINER=$EntitiesContainer" | Out-File -FilePath $env:GITHUB_ENV -Append
            
            Write-Verbose "Successfully configured Cosmos DB for tests"
        }
        catch {
            Write-Error "Cosmos DB discovery/configuration failed: $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Set-CosmosDbForTests process completed"
    }
}

# Call the function with script parameters when run as script
if ($MyInvocation.InvocationName -ne '.') {
    Set-CosmosDbForTests @PSBoundParameters
}