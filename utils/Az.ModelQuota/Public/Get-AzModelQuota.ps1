<#
.SYNOPSIS
    Gets Azure OpenAI model quota information for a specified region.

.DESCRIPTION
    This function retrieves quota information for Azure OpenAI (Cognitive Services) 
    models in the specified Azure region and subscription.

.PARAMETER Region
    The Azure region to check for quota information (e.g., "swedencentral", "eastus").

.PARAMETER SubscriptionId
    The Azure subscription ID to query for quota information.

.EXAMPLE
    Get-AzLLMModelQuota -Region "swedencentral" -SubscriptionId "c7f8ca1e-46f6-4a59-a039-15eaefd2337e"
    
    Gets the LLM model quota for Sweden Central region in the specified subscription.

.NOTES
    Requires the Az.CognitiveServices and Az.Accounts PowerShell modules.
    You must be authenticated to Azure using Connect-AzAccount before running this function.
#>
function Get-AzModelQuota {
    [CmdletBinding()]
    [OutputType([System.Object[]])]
    param(
        [Parameter(
            Mandatory = $true,
            ValueFromPipeline = $false,
            HelpMessage = "Azure region name (e.g., swedencentral, eastus)"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Region,

        [Parameter(
            Mandatory = $true,
            ValueFromPipeline = $false,
            HelpMessage = "Azure subscription ID"
        )]
        [ValidateScript({
            if ($_ -match '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
                $true
            }
            else {
                throw "SubscriptionId must be a valid GUID format"
            }
        })]
        [string]$SubscriptionId
    )

    begin {
        Write-Verbose "Starting Get-AzLLMModelQuota function"
        
        # Check if user is authenticated
        $context = Get-AzContext
        if (-not $context) {
            throw "Not authenticated to Azure. Please run 'Connect-AzAccount' first."
        }
    }

    process {
        try {
            Write-Verbose "Setting subscription context to: $SubscriptionId"
            $null = Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop

            Write-Verbose "Retrieving Cognitive Services accounts in region: $Region"
            
            # Get all Cognitive Services accounts in the specified region
            $cognitiveAccounts = Get-AzCognitiveServicesAccount | Where-Object { 
                $_.Location -eq $Region -and $_.Kind -eq 'OpenAI' 
            }

            if (-not $cognitiveAccounts) {
                Write-Warning "No Azure OpenAI accounts found in region: $Region"
                return @()
            }

            $quotaResults = @()

            foreach ($account in $cognitiveAccounts) {
                Write-Verbose "Getting quota information for account: $($account.Name)"
                
                try {
                    # Get usage information for the account
                    $usage = Get-AzCognitiveServicesAccountUsage -ResourceGroupName $account.ResourceGroupName -Name $account.Name
                    
                    # Get available SKUs for the account
                    $skus = Get-AzCognitiveServicesAccountSku -ResourceGroupName $account.ResourceGroupName -Name $account.Name

                    $quotaInfo = [PSCustomObject]@{
                        AccountName = $account.Name
                        ResourceGroup = $account.ResourceGroupName
                        Location = $account.Location
                        Kind = $account.Kind
                        Usage = $usage
                        AvailableSkus = $skus
                        Endpoint = $account.Endpoint
                    }

                    $quotaResults += $quotaInfo
                }
                catch {
                    Write-Warning "Failed to get quota information for account '$($account.Name)': $($_.Exception.Message)"
                }
            }

            Write-Verbose "Found $($quotaResults.Count) accounts with quota information"
            return $quotaResults
        }
        catch {
            Write-Error "Failed to retrieve quota information: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Completed Get-AzLLMModelQuota function"
    }
}
