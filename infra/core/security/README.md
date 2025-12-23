# Security Module README

This directory contains custom security modules for Azure infrastructure deployment.

## Available Modules

### role_aisearch.bicep

Creates data plane role assignments on an Azure AI Search service.

### role_foundry.bicep

Creates role assignments on an Azure AI Foundry (AI Services) account.

## Cosmos DB Data Plane Role Assignments

> **Note**: As of AVM version 0.18.0, Cosmos DB data plane role assignments are now supported natively through the `dataPlaneRoleAssignments` parameter in the main Cosmos DB module. The separate `role_cosmosdb.bicep` module has been removed.

### Supported Built-in Data Plane Roles

- **Cosmos DB Built-in Data Contributor** (`00000000-0000-0000-0000-000000000002`) - Full read/write access to data
- **Cosmos DB Built-in Data Reader** (`00000000-0000-0000-0000-000000000001`) - Read-only access to data

### Usage Example

```bicep
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.18.0' = {
  name: 'cosmos-db-deployment'
  params: {
    name: cosmosDbAccountName
    location: location
    // ... other parameters ...
    dataPlaneRoleAssignments: [
      {
        principalId: principalId
        roleDefinitionId: '00000000-0000-0000-0000-000000000002' // Cosmos DB Built-in Data Contributor
      }
    ]
  }
}
```

### Key Features

1. **Native Support**: Data plane role assignments are now natively supported in the AVM Cosmos DB module
2. **Simplified Deployment**: No need for separate module or complex role definition ID construction
3. **Built-in Role GUIDs**: Use the standard Cosmos DB built-in role definition IDs directly

### Notes

- Data plane role assignments in Cosmos DB are different from control plane (Azure RBAC) roles
- Role assignments are scoped to the entire Cosmos DB account
- The Cosmos DB account must exist before creating role assignments (handled by module dependencies)
