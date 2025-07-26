<#
    .SYNOPSIS
        Test Helper module for GenAI Database Explorer Console Integration Tests

    .DESCRIPTION
        This module provides common fixture support functions for integration testing
        of the GenAI Database Explorer Console application. It includes functions for
        project initialization, settings configuration, command execution, and test
        data creation.

    .NOTES
        Module: TestHelper
        Author: GenAI Database Explorer Team
        Version: 1.0.0
        PowerShell Version: 7+
        Dependencies: None

    .EXAMPLE
        Import-Module -Name .\TestHelper\TestHelper.psm1
        $result = Initialize-TestProject -ProjectPath "C:\temp\test" -ConsoleApp ".\app.exe"
#>

#Requires -Version 7

using namespace System.Management.Automation

function Initialize-TestProject {
    <#
    .SYNOPSIS
        Initializes a new test project using the console application.

    .DESCRIPTION
        Creates a new project using the GenAI Database Explorer console application
        and returns information about the initialization result.

    .PARAMETER ProjectPath
        The path where the project should be created.

    .PARAMETER ConsoleApp
        The path to the console application executable.

    .OUTPUTS
        Returns a hashtable with InitResult, ExitCode, and ProjectPath properties.

    .EXAMPLE
        $result = Initialize-TestProject -ProjectPath "C:\temp\testproject" -ConsoleApp ".\GenAIDBExplorer.Console.exe"
        if ($result.ExitCode -eq 0) {
            Write-Host "Project initialized successfully at $($result.ProjectPath)"
        }

    .NOTES
        This function executes the 'init-project' command of the console application
        and captures both the output and exit code for validation in tests.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsoleApp
    )

    Write-Verbose "Initializing test project at: $ProjectPath" -Verbose
    $initResult = & $ConsoleApp init-project --project $ProjectPath 2>&1
    $exitCode = $LASTEXITCODE

    return @{
        InitResult = $initResult
        ExitCode = $exitCode
        ProjectPath = $ProjectPath
    }
}

function Set-ProjectSettings {
    <#
    .SYNOPSIS
        Configures project settings with connection strings and AI endpoints.

    .DESCRIPTION
        Updates the settings.json file in a GenAI Database Explorer project with
        database connection strings and Azure OpenAI configuration.

    .PARAMETER ProjectPath
        The path to the project containing settings.json.

    .PARAMETER ConnectionString
        The database connection string to configure.

    .PARAMETER AzureOpenAIEndpoint
        The Azure OpenAI endpoint URL.

    .PARAMETER AzureOpenAIApiKey
        The Azure OpenAI API key (optional).

    .EXAMPLE
        Set-ProjectSettings -ProjectPath "C:\temp\project" -ConnectionString "Server=localhost;Database=Test;Integrated Security=true"

    .EXAMPLE
        Set-ProjectSettings -ProjectPath "C:\temp\project" -AzureOpenAIEndpoint "https://myopenai.openai.azure.com" -AzureOpenAIApiKey "abc123"

    .NOTES
        This function modifies the settings.json file in place. If the file doesn't exist,
        a warning is issued and the function returns without making changes.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ProjectPath,

        [Parameter()]
        [string]$ConnectionString,

        [Parameter()]
        [string]$AzureOpenAIEndpoint,

        [Parameter()]
        [string]$AzureOpenAIApiKey
    )

    $settingsPath = Join-Path -Path $ProjectPath -ChildPath 'settings.json'

    if (-not (Test-Path -Path $settingsPath)) {
        Write-Warning "Settings file not found at: $settingsPath"
        return
    }

    $settings = Get-Content -Path $settingsPath | ConvertFrom-Json

    if ($ConnectionString) {
        # Ensure MultipleActiveResultSets=True is included for the GenAI Database Explorer
        if ($ConnectionString -notmatch 'MultipleActiveResultSets\s*=') {
            # Add MultipleActiveResultSets=True if not present
            $ConnectionString = if ($ConnectionString.EndsWith(';')) {
                $ConnectionString + 'MultipleActiveResultSets=True;'
            } else {
                $ConnectionString + ';MultipleActiveResultSets=True;'
            }
            Write-Verbose "Added MultipleActiveResultSets=True to connection string" -Verbose
        }
        
        $settings.Database.ConnectionString = $ConnectionString
        Write-Verbose "Connection string configured in settings.json" -Verbose
    }

    # Set the Schema to empty to ensure all schemas are used
    $settings.Database.Schema = ''

    if ($AzureOpenAIEndpoint) {
        $settings.OpenAIService.Default.AzureOpenAIEndpoint = $AzureOpenAIEndpoint
        Write-Verbose "Azure OpenAI endpoint configured in settings.json" -Verbose
    }

    if ($AzureOpenAIApiKey) {
        $settings.OpenAIService.Default.AzureOpenAIKey = $AzureOpenAIApiKey
        Write-Verbose "Azure OpenAI API key configured in settings.json" -Verbose
    }

    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $settingsPath
}

function Invoke-ConsoleCommand {
    <#
    .SYNOPSIS
        Executes a console application command and captures output and exit code.

    .DESCRIPTION
        Provides a standardized way to execute the GenAI Database Explorer console
        application with arguments, capturing both output and exit codes for test validation.

    .PARAMETER ConsoleApp
        The path to the console application executable.

    .PARAMETER Arguments
        The arguments to pass to the console application.

    .OUTPUTS
        Returns a hashtable with Output, ExitCode, and Command properties.

    .EXAMPLE
        $result = Invoke-ConsoleCommand -ConsoleApp ".\app.exe" -Arguments @('extract-model', '--project', 'C:\temp\project')
        if ($result.ExitCode -eq 0) {
            Write-Host "Command succeeded: $($result.Command)"
        }

    .NOTES
        This function centralizes command execution logging and provides consistent
        output capture patterns for all console application interactions in tests.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsoleApp,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [string[]]$Arguments
    )

    $commandLine = "$ConsoleApp $($Arguments -join ' ')"
    Write-Verbose "Executing: $commandLine" -Verbose

    $result = & $ConsoleApp @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    Write-Verbose "Console Output:" -Verbose
    foreach ($line in $result) {
        Write-Verbose "  $line" -Verbose
    }
    Write-Verbose "Exit Code: $exitCode" -Verbose

    return @{
        Output = $result
        ExitCode = $exitCode
        Command = $commandLine
    }
}

function New-TestDataDictionary {
    <#
    .SYNOPSIS
        Creates a sample data dictionary file for testing.

    .DESCRIPTION
        Generates a JSON data dictionary file with the specified database object
        information for use in integration tests of the data dictionary functionality.

    .PARAMETER DictionaryPath
        The path where the dictionary file should be created.

    .PARAMETER ObjectType
        The type of database object (e.g., 'table').

    .PARAMETER SchemaName
        The schema name for the object.

    .PARAMETER ObjectName
        The name of the database object.

    .PARAMETER Description
        The description for the object.

    .EXAMPLE
        New-TestDataDictionary -DictionaryPath "C:\temp\dict.json" -ObjectType "table" -SchemaName "dbo" -ObjectName "Customer" -Description "Customer information"

    .NOTES
        This function creates the parent directory if it doesn't exist and generates
        a standardized JSON structure that matches the expected data dictionary format.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$DictionaryPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ObjectType,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$SchemaName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ObjectName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Description
    )

    $dictionaryDir = Split-Path -Path $DictionaryPath -Parent
    if (-not (Test-Path -Path $dictionaryDir)) {
        New-Item -ItemType Directory -Path $dictionaryDir -Force | Out-Null
    }

    $sampleDict = @{
        objectType = $ObjectType
        schemaName = $SchemaName
        objectName = $ObjectName
        description = $Description
        columns = @(
            @{
                name = 'ID'
                description = 'Unique identifier'
            }
        )
    }

    $sampleDict | ConvertTo-Json -Depth 3 | Set-Content -Path $DictionaryPath
    Write-Verbose "Created test data dictionary at: $DictionaryPath" -Verbose
}

# Export all functions
Export-ModuleMember -Function Initialize-TestProject, Set-ProjectSettings, Invoke-ConsoleCommand, New-TestDataDictionary
