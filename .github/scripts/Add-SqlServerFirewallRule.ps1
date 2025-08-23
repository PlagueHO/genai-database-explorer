<#
.SYNOPSIS
Adds a SQL Server firewall rule for the current GitHub Actions runner IP address.

.DESCRIPTION
Retrieves the current runner's public IP address and creates a SQL Server firewall rule to allow connections.
Checks for existing rules and avoids duplicates. Designed for temporary access during CI/CD operations.

.PARAMETER ResourceGroupName
The Azure resource group containing the SQL Server.

.PARAMETER SqlServerName
The name of the Azure SQL Server.

.PARAMETER FirewallRuleName
The name to assign to the firewall rule. Should be unique within the SQL Server.

.PARAMETER Force
If specified, removes existing rules with the same name before creating the new rule.

.EXAMPLE
./Add-SqlServerFirewallRule.ps1 -ResourceGroupName 'rg-test' -SqlServerName 'sql-test' -FirewallRuleName 'GitHubRunner-123'

.EXAMPLE
./Add-SqlServerFirewallRule.ps1 -ResourceGroupName 'rg-test' -SqlServerName 'sql-test' -FirewallRuleName 'GitHubRunner-123' -Force

.OUTPUTS
None. Creates a firewall rule allowing the current runner IP address.

.NOTES
This script is designed for CI/CD scenarios and requires Azure PowerShell modules.
The firewall rule should be removed after testing using Remove-SqlServerFirewallRule.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlServerName,
    
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$FirewallRuleName,
    
    [Parameter()]
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Verbose "Starting SQL Server firewall rule addition"

# Get the current runner's public IP address
function Get-RunnerIPAddress {
    [CmdletBinding()]
    param()
    
    try {
        $ip = (Invoke-RestMethod -Uri "https://ipinfo.io/ip" -Method Get).Trim()
        Write-Verbose "Retrieved runner IP address: $ip"
        return $ip
    }
    catch {
        Write-Error "Failed to retrieve runner IP address: $($_.Exception.Message)"
        throw
}

try {
    # Get the current runner's public IP address
    $runnerIP = Get-RunnerIPAddress
    Write-Host "GitHub Runner IP: $runnerIP"
    
    Write-Host "Adding firewall rule for SQL Server: $SqlServerName"
    Write-Host "Resource Group: $ResourceGroupName"
    Write-Host "Runner IP: $runnerIP"
    Write-Host "Firewall Rule Name: $FirewallRuleName"
    
    # Check if the rule already exists
    $existingRule = $null
    try {
        $existingRule = Get-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -FirewallRuleName $FirewallRuleName -ErrorAction SilentlyContinue
        if ($existingRule) {
            if ($Force) {
                Write-Host "Existing firewall rule found. Removing due to -Force parameter..." -ForegroundColor Yellow
                if ($PSCmdlet.ShouldProcess($FirewallRuleName, "Remove existing SQL Server firewall rule")) {
                    Remove-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -FirewallRuleName $FirewallRuleName -Force
                    Write-Verbose "Existing rule removed successfully"
                }
            } else {
                Write-Host "Firewall rule already exists with same name and IP range - skipping creation" -ForegroundColor Yellow
                return
            }
        }
    }
    catch {
        # Rule doesn't exist, continue with creation
        Write-Verbose "No existing rule found, proceeding with creation"
    }
    
    # Add firewall rule for the GitHub runner IP
    if ($PSCmdlet.ShouldProcess("$SqlServerName/$FirewallRuleName", "Add SQL Server firewall rule for IP $runnerIP")) {
        New-AzSqlServerFirewallRule `
            -ResourceGroupName $ResourceGroupName `
            -ServerName $SqlServerName `
            -FirewallRuleName $FirewallRuleName `
            -StartIpAddress $runnerIP `
            -EndIpAddress $runnerIP
        
        Write-Host "✅ Firewall rule created successfully" -ForegroundColor Green
        Write-Verbose "Firewall rule '$FirewallRuleName' created for IP $runnerIP"
    }
}
catch {
    if ($_.Exception.Message -like "*already exists*") {
        Write-Warning "Firewall rule already exists - this may be due to parallel job execution"
        Write-Host "Continuing with existing rule" -ForegroundColor Yellow
    }
    else {
        Write-Error "Failed to create firewall rule: $($_.Exception.Message)"
        throw
    }
}

Write-Verbose "SQL Server firewall rule addition completed"