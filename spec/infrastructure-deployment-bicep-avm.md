---
title: Infrastructure Deployment using Bicep and Azure Verified Modules
version: 1.1  
date_created: 2025-06-07  
last_updated: 2025-06-28
owner: Platform Engineering Team
tags: [infrastructure, bicep, avm, azure, deployment]
---

## Introduction

Defines requirements for deploying infrastructure using Bicep and Azure Verified Modules (AVM) via Azure Developer CLI.

## 1. Purpose & Scope

Standardizes infrastructure deployment using Bicep and AVM for the GenAI Database Explorer solution. Covers `main.bicep` and `main.bicepparam` structure, requirements, and validation. Target audience: platform engineers, DevOps, and AI agents.

## 2. Definitions

- **Bicep**: A domain-specific language (DSL) for deploying Azure resources declaratively.
- **AZD (Azure Developer CLI)**: A command-line interface for deploying Azure resources using Bicep and other tools.
- **AVM (Azure Verified Modules)**: Pre-built, Microsoft-verified Bicep modules for common Azure resources, available at [https://aka.ms/avm](https://aka.ms/avm).
- **IaC**: Infrastructure as Code.
- **Parameter File**: A `.bicepparam` file providing input values for Bicep deployments.

## 3. Requirements, Constraints & Guidelines

- **REQ-001**: All core Azure resources must be deployed using AVM modules where available.
- **REQ-002**: The `main.bicep` file must follow the structure and modularity pattern of the [Azure AI Foundry Jumpstart main.bicep](https://github.com/PlagueHO/azure-ai-foundry-jumpstart/blob/main/infra/main.bicep).
- **REQ-003**: A `main.bicepparam` file must exist in the same directory as `main.bicep` and provide all required parameters for deployment.
- **REQ-004**: Must comply with requirements for being deployed via Azure Developer CLI, including parameterization and secure handling of secrets.
- **REQ-005**: The `main.bicepparam` file must read all parameter values from environment variables using `readEnvironmentVariable()` function with Azure Developer CLI naming conventions (uppercase with underscoreseparators) and provide appropriate default values.
- **REQ-006**: The deployment must support conditional resource deployment using boolean parameters (e.g., Azure AI Search deployment).
- **REQ-007**: All deployed resources must be tagged with a common set of tags including environment name and project identifier.
- **REQ-008**: The deployment must output essential resource identifiers and connection strings for consumption by applications.
- **REQ-009**: The deployment must use subscription-level scope to enable resource group creation as part of the deployment.
- **SEC-001**: All secrets and sensitive values must be passed as secure parameters and never hardcoded.
- **SEC-002**: Role-based access control (RBAC) and network security rules must be defined using AVM modules where possible.
- **SEC-003**: Azure Key Vault must be used to store secrets wherever possible. Secret parameters should reference Key Vault secrets using secure parameter patterns rather than passing secrets directly as parameters.
- **CON-001**: Only Microsoft-verified AVM modules from [https://aka.ms/avm](https://aka.ms/avm) may be used for core resources.
- **GUD-001**: Use parameterization and outputs to maximize reusability and composability.
- **GUD-002**: All Bicep files must pass `az bicep lint` validation with no errors or warnings to ensure code quality and adherence to best practices.
- **PAT-001**: Follow the folder and file naming conventions: `infra/main.bicep`, `infra/main.bicepparam`, and `infra/modules/` for custom modules if needed.

## 4. Interfaces & Data Contracts

### Core Template Structure

**main.bicep**: Subscription-scope template with parameters, variables, AVM modules, and outputs.
**main.bicepparam**: Parameter file using `readEnvironmentVariable()` with AZD conventions.

### Required Parameters

```bicep
param environmentName string // @minLength(1) @maxLength(40)
param location string // @metadata azd.type: location
param resourceGroupName string = 'rg-${environmentName}'
param principalId string
param principalIdType string = 'User' // @allowed: User|ServicePrincipal
param sqlServerUsername string
@secure() param sqlServerPassword string
param azureAiSearchDeploy bool = false
```

### AVM Module Pattern

```bicep
module <name> 'br/public:avm/res/<service>/<resource>:<version>' = {
  name: '<deployment-name>'
  scope: resourceGroup(resourceGroupName)
  params: { /* module-specific parameters */ }
}
```

### Required Outputs

```bicep
output AZURE_RESOURCE_GROUP string
output AZURE_AI_SERVICES_ENDPOINT string
output SQL_SERVER_NAME string
output AZURE_AI_SEARCH_NAME string // conditional
```

## 5. Acceptance Criteria

- **AC-001**: `az bicep lint` passes without errors/warnings
- **AC-002**: Environment variables resolve per AZD conventions (UPPERCASE_WITH_UNDERSCORES)
- **AC-003**: `azd up` deploys successfully using AVM modules
- **AC-004**: Secrets use `@secure()` decorator, never exposed in outputs
- **AC-005**: RBAC assignments created for specified principal
- **AC-006**: Logs flow to Log Analytics workspace
- **AC-007**: Files exist in `infra/` directory structure
- **AC-008**: Conditional deployment works for `azureAiSearchDeploy`

## 6. Test Automation Strategy

**Validation Levels**: Bicep linting → ARM validation → What-if analysis → Integration testing → Smoke testing

**Tools**: Azure CLI, Azure Bicep, Azure Developer CLI, GitHub Actions

**CI/CD Pipeline**:

- PR: Automated bicep linting
- Pre-deploy: Template validation and what-if
- Post-deploy: Resource verification and health checks

**Coverage**: Zero lint errors, environment variable tests, AVM version compatibility, RBAC verification

## 7. Rationale & Context

Using AVM modules ensures security, compliance, and maintainability by leveraging Microsoft-verified best practices. Standardizing the structure and naming conventions improves onboarding and automation for both humans and AI agents.

## 8. Examples & Edge Cases

### Core Deployment Pattern

```bicep
targetScope = 'subscription'
var abbrs = loadJsonContent('./abbreviations.json')
var tags = { 'azd-env-name': environmentName, project: 'genai-database-explorer' }

// Conditional deployment
module aiSearch 'br/public:avm/res/search/search-service:0.10.0' = if (azureAiSearchDeploy) {
  name: 'ai-search'
  params: { name: 'search-${environmentName}', location: location, tags: tags }
}

// Secure parameter usage
module sql 'br/public:avm/res/sql/server:0.17.0' = {
  params: { administratorLoginPassword: sqlServerPassword } // @secure() param
}
```

### Parameter File Template

```bicep
using './main.bicep'
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param sqlServerPassword = readEnvironmentVariable('SQL_SERVER_PASSWORD', '')
param azureAiSearchDeploy = bool(readEnvironmentVariable('AZURE_AI_SEARCH_DEPLOY', 'false'))
```

**Key Constraints**: `environmentName` 1-40 chars, `principalIdType` User|ServicePrincipal, secure params never in outputs

## 9. Validation Criteria

- AVM modules with current stable versions used for all core resources
- No hardcoded secrets; `@secure()` decorator for sensitive values
- `main.bicepparam` exists with proper environment variable mappings
- UPPERCASE_WITH_UNDERSCORES convention for AZD parameters
- Subscription-scope deployment with proper resource group creation
- Consistent tagging (environment name + project identifier)
- Essential outputs provided for application consumption
- `az bicep lint` passes with zero errors/warnings
- Conditional deployment works correctly
- AdventureWorksLT database configured with appropriate settings
- `azd up` deploys successfully

## 10. Related Specifications / Further Reading

- [Azure Verified Modules](https://aka.ms/avm)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure AI Foundry Jumpstart main.bicep](https://github.com/PlagueHO/azure-ai-foundry-jumpstart/blob/main/infra/v1/main.bicep)
