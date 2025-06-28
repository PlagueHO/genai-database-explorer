---
goal: Upgrade System.CommandLine from 2.0.0-beta4.22272.1 to 2.0.0-beta5.25306.1
version: 1.0
date_created: 2025-06-25
last_updated: 2025-06-25
owner: GenAI Database Explorer Team
tags: [upgrade, dependency, breaking-changes, command-line]
---

# Introduction

This plan outlines the steps required to upgrade the System.CommandLine package in the GenAIDBExplorer.Console project from version 2.0.0-beta4.22272.1 to 2.0.0-beta5.25306.1. This upgrade introduces significant breaking changes that require code modifications across all command handlers and the main program entry point.

## 1. Requirements & Constraints

- **REQ-001**: Update System.CommandLine package reference to version 2.0.0-beta5.25306.1
- **REQ-002**: Maintain existing command-line interface behavior and functionality
- **REQ-003**: Preserve all current command options, aliases, and descriptions
- **REQ-004**: Ensure all command handlers continue to work with the new API
- **REQ-005**: Maintain dependency injection integration with IHost
- **SEC-001**: Ensure no security regressions are introduced during the upgrade
- **CON-001**: Must maintain compatibility with .NET 9.0 target framework
- **CON-002**: Cannot change the public command-line interface exposed to users
- **GUD-001**: Follow the migration patterns outlined in Microsoft's official migration guide
- **GUD-002**: Use modern C# language features and constructs where applicable
- **PAT-001**: Maintain the existing command handler pattern and architecture

## 2. Implementation Steps

### Step 1: Update Package Reference

- Update the PackageReference in `GenAIDBExplorer.Console.csproj` from `2.0.0-beta4.22272.1` to `2.0.0-beta5.25306.1`

### Step 2: Update Program.cs Entry Point

- Replace `rootCommand.AddCommand()` calls with `rootCommand.Subcommands.Add()` syntax
- Update the main entry point to handle the new parsing and invocation separation

### Step 3: Update Command Handler Base Class

- Review and update `CommandHandler<TOptions>` base class if needed
- Ensure compatibility with new action-based approach

### Step 4: Update Individual Command Handlers

Update each command handler's `SetupCommand` method to use the new API:

#### InitProjectCommandHandler

- Replace `IsRequired = true` with `Required = true` for options
- Replace `AddOption()` with `Options.Add()` syntax
- Replace `SetHandler()` with `SetAction()` method
- Update handler signature to use `ParseResult` parameter

#### ExtractModelCommandHandler

- Apply same changes as InitProjectCommandHandler
- Update multiple option handling pattern
- Ensure boolean options with default values work correctly

#### DataDictionaryCommandHandler

- Update complex command structure with subcommands
- Replace `AddCommand()` with `Subcommands.Add()`
- Update `ArgumentHelpName` to `HelpName` property
- Update handler methods to use new action pattern

#### EnrichModelCommandHandler

- Apply standard option and handler updates
- Ensure any custom parsing logic is compatible

#### ExportModelCommandHandler

- Update file type option handling
- Ensure default value factories work correctly
- Update handler method signatures

#### QueryModelCommandHandler

- Apply standard updates for simple command structure

#### ShowObjectCommandHandler

- Apply standard updates
- Ensure any complex option handling is updated

### Step 5: Update Option Creation Patterns

- Replace constructor patterns that take name and description separately
- Use new constructor with name and aliases, set Description property separately
- Example: `new Option<bool>("--help", "-h", "/h") { Description = "Help option" }`

### Step 6: Update Handler Method Signatures

- Replace `SetHandler` with `SetAction`
- Update handler delegates to accept `ParseResult` instead of individual parameters
- Use `parseResult.GetValue<T>(option)` or `parseResult.GetValue<T>("optionName")` to get values
- For async handlers, ensure `CancellationToken` parameter is included where needed

### Step 7: Testing and Validation

- Build the project to identify any remaining compilation errors
- Run all existing commands to ensure functionality is preserved
- Verify help output matches expected format
- Test error handling and validation scenarios
- Validate performance is acceptable

### Step 8: Documentation Updates

- Update any internal documentation referencing the old API patterns
- Review code comments for accuracy with new implementation

## 3. Alternatives

- **ALT-001**: Stay with System.CommandLine 2.0.0-beta4 - Not recommended due to lack of future support and missing improvements
- **ALT-002**: Switch to alternative command-line parsing library (e.g., CommandLineParser) - Would require complete rewrite and doesn't align with Microsoft ecosystem preference
- **ALT-003**: Implement custom command-line parsing - Would increase maintenance burden and lose System.CommandLine benefits

## 4. Dependencies

- **DEP-001**: .NET 9.0 SDK for building and testing
- **DEP-002**: Microsoft.Extensions.Hosting package compatibility with new System.CommandLine version
- **DEP-003**: System.CommandLine 2.0.0-beta5.25306.1 package availability
- **DEP-004**: All existing project dependencies must remain compatible

## 5. Files

- **FILE-001**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj` - Update package reference
- **FILE-002**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/Program.cs` - Update main entry point and command registration
- **FILE-003**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/CommandHandler.cs` - Update base class if needed
- **FILE-004**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/InitProjectCommandHandler.cs` - Update option creation and handler setup
- **FILE-005**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ExtractModelCommandHandler.cs` - Update complex option handling
- **FILE-006**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/DataDictionaryCommandHandler.cs` - Update subcommand structure
- **FILE-007**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/EnrichModelCommandHandler.cs` - Update standard patterns
- **FILE-008**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ExportModelCommandHandler.cs` - Update file handling options
- **FILE-009**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/QueryModelCommandHandler.cs` - Update simple command pattern
- **FILE-010**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ShowObjectCommandHandler.cs` - Update standard patterns

## 6. Testing

- **TEST-001**: Unit tests for each command handler to verify option parsing works correctly
- **TEST-002**: Integration tests to verify command execution produces expected results
- **TEST-003**: Help system tests to ensure help output is correctly formatted
- **TEST-004**: Error handling tests to verify validation and error messages work properly
- **TEST-005**: Performance tests to ensure startup time and execution speed remain acceptable
- **TEST-006**: Manual testing of all command-line scenarios documented in user guides

## 7. Risks & Assumptions

- **RISK-001**: Breaking changes in System.CommandLine might introduce subtle behavioral differences not caught by testing
- **RISK-002**: Performance impact from the new API might affect user experience, though documentation suggests improvements
- **RISK-003**: Dependency compatibility issues with other packages in the solution
- **RISK-004**: Migration guide might not cover all edge cases present in the current implementation
- **ASSUMPTION-001**: All current functionality can be preserved with the new API
- **ASSUMPTION-002**: The dependency injection pattern with IHost will continue to work
- **ASSUMPTION-003**: No changes needed in the Core library that might depend on command-line parsing results
- **ASSUMPTION-004**: Current error handling and validation logic can be adapted to new API patterns

## 8. Related Specifications / Further Reading

- [System.CommandLine 2.0.0-beta5 Migration Guide](https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5)
- [System.CommandLine Overview](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [How to parse and invoke commands in System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke)
- [How to customize parsing and validation in System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-customize-parsing-and-validation)
