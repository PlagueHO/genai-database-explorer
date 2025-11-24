# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10 upgrade.
3. Upgrade GenAIDBExplorer.Core.csproj
4. Upgrade GenAIDBExplorer.Console.csproj
5. Upgrade GenAIDBExplorer.Core.Test.csproj
6. Upgrade GenAIDBExplorer.Console.Test.csproj
7. Run unit tests to validate upgrade in the projects listed below:
   - GenAIDBExplorer.Core.Test.csproj
   - GenAIDBExplorer.Console.Test.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                              | Current Version | New Version | Description                                   |
|:----------------------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Azure.Identity                                            | 1.17.0          | 1.17.1      | Deprecated version with MSAL dependency       |
| Microsoft.Extensions.Caching.Memory                       | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Configuration                        | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Configuration.Binder                 | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Configuration.FileExtensions         | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Configuration.Json                   | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.DependencyInjection                  | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Hosting                              | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Logging                              | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |
| Microsoft.Extensions.Options.ConfigurationExtensions      | 9.0.10          | 10.0.0      | Recommended for .NET 10                       |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### GenAIDBExplorer.Core.csproj modifications

Project properties changes:
- Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
- Azure.Identity should be updated from `1.17.0` to `1.17.1` (*deprecated version with MSAL dependency*)
- Microsoft.Extensions.Caching.Memory should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Configuration should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Configuration.Binder should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Configuration.FileExtensions should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Configuration.Json should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.DependencyInjection should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Logging should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Options.ConfigurationExtensions should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)

#### GenAIDBExplorer.Console.csproj modifications

Project properties changes:
- Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
- Microsoft.Extensions.Configuration should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.DependencyInjection should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Hosting should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)
- Microsoft.Extensions.Logging should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)

#### GenAIDBExplorer.Core.Test.csproj modifications

Project properties changes:
- Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
- Microsoft.Extensions.Hosting should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10*)

#### GenAIDBExplorer.Console.Test.csproj modifications

Project properties changes:
- Target framework should be changed from `net9.0` to `net10.0`
