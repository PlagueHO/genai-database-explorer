# Az.ModelQuota Module

This PowerShell module provides functions for retrieving Azure OpenAI model quota information.

## Installation

To install this module locally:

```powershell
# Navigate to the module directory
cd "d:\source\GitHub\PlagueHO\genai-database-explorer\utils\Az.ModelQuota"

# Import the module
Import-Module .\Az.ModelQuota.psd1
```

## Prerequisites

This module requires:

- Az.CognitiveServices PowerShell module
- Az.Accounts PowerShell module
- Azure authentication (run `Connect-AzAccount` first)

## Functions

### Get-AzLLMModelQuota

Gets Azure OpenAI model quota information for a specified region.

#### Parameters

- **Region** (Required): The Azure region to check for quota information (e.g., "swedencentral", "eastus")
- **SubscriptionId** (Required): The Azure subscription ID to query for quota information

#### Example

```powershell
Get-AzLLMModelQuota -Region "swedencentral" -SubscriptionId "c7f8ca1e-46f6-4a59-a039-15eaefd2337e"
```

This command gets the LLM model quota for Sweden Central region in the specified subscription.

#### Output

Returns an array of objects containing:

- AccountName: Name of the Azure OpenAI account
- ResourceGroup: Resource group containing the account
- Location: Azure region
- Kind: Service type (OpenAI)
- Usage: Current usage information
- AvailableSkus: Available pricing tiers/SKUs
- Endpoint: Service endpoint URL

## Notes

- You must be authenticated to Azure using `Connect-AzAccount` before running any functions in this module
- The function will automatically set the subscription context to the specified subscription ID
- If no Azure OpenAI accounts are found in the specified region, a warning will be displayed and an empty array returned
