function Remove-SqlServerFirewallRule {
    <#
    .SYNOPSIS
    Removes a SQL Server firewall rule created for GitHub Actions runner access.

    .DESCRIPTION
    Safely removes a SQL Server firewall rule with error handling for cases where the rule
    may not exist or has already been removed. Designed for cleanup operations in CI/CD pipelines.

    .PARAMETER ResourceGroupName
    The Azure resource group containing the SQL Server.

    .PARAMETER SqlServerName
    The name of the Azure SQL Server.

    .PARAMETER FirewallRuleName
    The name of the firewall rule to remove.

    .PARAMETER IgnoreNotFound
    If specified, suppresses warnings when the firewall rule is not found.

    .EXAMPLE
    Remove-SqlServerFirewallRule -ResourceGroupName 'rg-test' -SqlServerName 'sql-test' -FirewallRuleName 'GitHubRunner-123'

    .EXAMPLE
    Remove-SqlServerFirewallRule -ResourceGroupName 'rg-test' -SqlServerName 'sql-test' -FirewallRuleName 'GitHubRunner-123' -IgnoreNotFound

    .OUTPUTS
    None. Removes the specified SQL Server firewall rule.

    .NOTES
    This function is designed for cleanup operations in CI/CD scenarios and requires Azure PowerShell modules.
    It gracefully handles cases where the rule doesn't exist or has already been removed.
    #>
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
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
        [switch]$IgnoreNotFound
    )
    
    begin {
        Set-StrictMode -Version Latest
        $ErrorActionPreference = 'Stop'
        
        Write-Verbose "Starting Remove-SqlServerFirewallRule process"
    }
    
    process {
        try {
            Write-Host "Removing firewall rule for SQL Server: $SqlServerName"
            Write-Host "Resource Group: $ResourceGroupName"
            Write-Host "Firewall Rule Name: $FirewallRuleName"
            
            # Check if the rule exists before attempting removal
            $existingRule = $null
            try {
                $existingRule = Get-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -FirewallRuleName $FirewallRuleName -ErrorAction SilentlyContinue
            }
            catch {
                Write-Verbose "Error checking for existing rule: $($_.Exception.Message)"
            }
            
            if ($existingRule) {
                Write-Verbose "Firewall rule exists, proceeding with removal"
                
                if ($PSCmdlet.ShouldProcess("$SqlServerName/$FirewallRuleName", "Remove SQL Server firewall rule")) {
                    # Remove the firewall rule
                    Remove-AzSqlServerFirewallRule `
                        -ResourceGroupName $ResourceGroupName `
                        -ServerName $SqlServerName `
                        -FirewallRuleName $FirewallRuleName `
                        -Force
                    
                    Write-Host "✅ Firewall rule removed successfully" -ForegroundColor Green
                    Write-Verbose "Successfully removed firewall rule '$FirewallRuleName'"
                }
            }
            else {
                $message = "Firewall rule '$FirewallRuleName' not found or already removed"
                if ($IgnoreNotFound) {
                    Write-Verbose $message
                } else {
                    Write-Warning $message
                    Write-Host "This is expected if the rule was already cleaned up or never created"
                }
            }
        }
        catch {
            # Ignore errors if rule doesn't exist or already removed
            $errorMessage = $_.Exception.Message
            if ($errorMessage -like "*not found*" -or $errorMessage -like "*does not exist*") {
                $message = "Firewall rule not found or already removed: $errorMessage"
                if ($IgnoreNotFound) {
                    Write-Verbose $message
                } else {
                    Write-Warning $message
                    Write-Host "This is expected if the rule was already cleaned up or never created"
                }
            }
            else {
                Write-Error "Failed to remove firewall rule: $errorMessage"
                throw
            }
        }
    }
    
    end {
        Write-Host "Firewall rule cleanup completed"
        Write-Verbose "Remove-SqlServerFirewallRule process completed"
    }
}

# Call the function with script parameters when run as script
if ($MyInvocation.InvocationName -ne '.') {
    Remove-SqlServerFirewallRule @PSBoundParameters
}