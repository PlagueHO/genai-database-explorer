targetScope = 'subscription'

@sys.description('Name of the the environment which is used to generate a short unique hash used in all resources.')
@minLength(1)
@maxLength(40)
param environmentName string

@sys.description('Location for all resources')
@minLength(1)
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@sys.description('The Azure resource group where new resources will be deployed.')
@metadata({
  azd: {
    type: 'resourceGroup'
  }
})
param resourceGroupName string = 'rg-${environmentName}'

@sys.description('Id of the user or app to assign application roles.')
param principalId string

@sys.description('Type of the principal referenced by principalId.')
@allowed([
  'User'
  'ServicePrincipal'
])
param principalIdType string = 'User'

@sys.description('The SQL logical server administrator username.')
param sqlServerUsername string

@sys.description('The SQL logical server administrator password.')
@secure()
param sqlServerPassword string

@sys.description('Whether to deploy Azure AI Search service.')
param azureAiSearchDeploy bool = false

@sys.description('Whether to deploy Cosmos DB.')
param cosmosDbDeploy bool = false

@sys.description('Whether to deploy Storage Account.')
param storageAccountDeploy bool = false

var abbrs = loadJsonContent('./abbreviations.json')
var openAiModels = loadJsonContent('./azure-openai-models.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
  project: 'genai-database-explorer'
}

// Generate a unique token to be used in naming resources.
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var logAnalyticsWorkspaceName = '${abbrs.operationalInsightsWorkspaces}${environmentName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var aiFoundryName = '${abbrs.aiFoundryAccounts}${environmentName}'
var aiFoundryCustomSubDomainName = toLower(replace(environmentName, '-', ''))
var aiSearchName = '${abbrs.aiSearchSearchServices}${environmentName}'
var cosmosDbAccountName = '${abbrs.cosmosDBAccounts}${environmentName}'
var storageAccountName = '${abbrs.storageStorageAccounts}${toLower(replace(environmentName, '-', ''))}'

// Use the OpenAI models directly from JSON - they're already in the correct format for the AVM module
var openAiModelDeployments = openAiModels

// The application resources that are deployed into the application resource group
module rg 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'resource-group-deployment-${resourceToken}'
  params: {
    name: resourceGroupName
    location: location
    tags: tags
  }
}

// --------- MONITORING RESOURCES ---------
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.12.0' = {
  name: 'log-analytics-workspace-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }

}

module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'application-insights-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// --------- AI FOUNDRY ---------
module aiFoundryService './cognitive-services/accounts/main.bicep' = {
  name: 'ai-foundry-service-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: aiFoundryName
    kind: 'AIServices'
    location: location
    customSubDomainName: aiFoundryCustomSubDomainName
    disableLocalAuth: false
    allowProjectManagement: true
    diagnosticSettings: [
      {
        name: 'send-to-log-analytics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    sku: 'S0'
    deployments: openAiModelDeployments
    tags: tags
  }
}

// Safe extraction of optional module outputs to avoid BCP318 warnings
#disable-next-line BCP318 use-safe-access // Module is guaranteed to exist when azureAiSearchDeploy is true
var aiSearchServiceMIPrincipalId = azureAiSearchDeploy ? aiSearchService.outputs.systemAssignedMIPrincipalId : ''
#disable-next-line BCP318 // Module is guaranteed to exist when azureAiSearchDeploy is true
var aiSearchServiceName = azureAiSearchDeploy ? aiSearchService.outputs.name : ''
#disable-next-line BCP318 // Module is guaranteed to exist when azureAiSearchDeploy is true
var aiSearchServiceResourceId = azureAiSearchDeploy ? aiSearchService.outputs.resourceId : ''
#disable-next-line BCP318 // Module is guaranteed to exist when cosmosDbDeploy is true
var cosmosDbAccountNameOut = cosmosDbDeploy ? cosmosDbAccount.outputs.name : ''
#disable-next-line BCP318 // Module is guaranteed to exist when cosmosDbDeploy is true
var cosmosDbAccountResourceIdOut = cosmosDbDeploy ? cosmosDbAccount.outputs.resourceId : ''
#disable-next-line BCP318 // Module is guaranteed to exist when cosmosDbDeploy is true
var cosmosDbAccountEndpointOut = cosmosDbDeploy ? cosmosDbAccount.outputs.endpoint : ''
#disable-next-line BCP318 // Module is guaranteed to exist when storageAccountDeploy is true
var storageAccountNameOut = storageAccountDeploy ? storageAccount.outputs.name : ''
#disable-next-line BCP318 // Module is guaranteed to exist when storageAccountDeploy is true
var storageAccountResourceIdOut = storageAccountDeploy ? storageAccount.outputs.resourceId : ''
#disable-next-line BCP318 // Module is guaranteed to exist when storageAccountDeploy is true
var storageAccountBlobEndpointOut = storageAccountDeploy ? storageAccount.outputs.primaryBlobEndpoint : ''

// Add role assignments for AI Services using the role_aiservice.bicep module
// This needs to be done after the AI Services account is created to avoid circular dependencies
// between the AI Services account and the AI Search service.
var aiFoundryRoleAssignmentsArray = [
  // search–specific roles only when search is present
  ...(azureAiSearchDeploy ? [
    {
      roleDefinitionIdOrName: 'Cognitive Services Contributor'
      principalType: 'ServicePrincipal'
      principalId: aiSearchServiceMIPrincipalId
    }
    {
      roleDefinitionIdOrName: 'Cognitive Services OpenAI Contributor'
      principalType: 'ServicePrincipal'
      principalId: aiSearchServiceMIPrincipalId
    }
  ] : [])
  // Developer role assignments
  ...(!empty(principalId) ? [
    {
      roleDefinitionIdOrName: 'Contributor'
      principalType: principalIdType
      principalId: principalId
    }
    {
      roleDefinitionIdOrName: 'Cognitive Services OpenAI Contributor'
      principalType: principalIdType
      principalId: principalId
    }
  ] : [])
]

module aiFoundryRoleAssignments './core/security/role_aifoundry.bicep' = {
  name: 'ai-foundry-role-assignments-${resourceToken}'
  scope: az.resourceGroup(resourceGroupName)
  dependsOn: [
    rg
    aiFoundryService
  ]
  params: {
    azureAiFoundryName: aiFoundryName
    roleAssignments: aiFoundryRoleAssignmentsArray
  }
}

// Map principalIdType to SQL administrator principalType
var sqlAdminPrincipalType = principalIdType == 'ServicePrincipal' ? 'Application' : (principalIdType == 'User' ? 'User' : 'Application')

// --------- SQL DATABASE ---------
module sqlServer 'br/public:avm/res/sql/server:0.20.0' = {
  name: 'sql-server-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: '${abbrs.sqlServers}${environmentName}'
    location: location
    administratorLogin: sqlServerUsername
    administratorLoginPassword: sqlServerPassword
    administrators: {
      azureADOnlyAuthentication: false
      login: principalId // Use the principal ID as the login name (can be email for users, application name for service principals)
      principalType: sqlAdminPrincipalType // 'Application', 'Group', or 'User'
      sid: principalId // The object ID (principal ID) of the administrator
      administratorType: 'ActiveDirectory'
      tenantId: tenant().tenantId // Current tenant ID
    }
    databases: [
      {
        name: 'AdventureWorksLT'
        availabilityZone: -1
        sku: {
          name: 'GP_S_Gen5_2'
          tier: 'GeneralPurpose'
        }
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        maxSizeBytes: 34359738368
        sampleName: 'AdventureWorksLT'
        zoneRedundant: false
        readScale: 'Disabled'
        highAvailabilityReplicaCount: 0
        minCapacity: '0.5'
        autoPauseDelay: 60
        requestedBackupStorageRedundancy: 'Local'
        isLedgerOn: false
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'SQL DB Contributor'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'SQL Security Manager'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- COSMOS DB ---------
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.15.0' = if (cosmosDbDeploy) {
  name: 'cosmos-db-account-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: cosmosDbAccountName
    location: location
    enableFreeTier: false
    sqlDatabases: [
      {
        name: 'genaidbexp'
        throughput: 400
        containers: [
          {
            name: 'default-container'
            paths: ['/id']
            throughput: 400
          }
        ]
      }
    ]
    diagnosticSettings: [
      {
        name: 'send-to-log-analytics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Cosmos DB Account Reader Role'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// Add data plane role assignments for Cosmos DB using the new role_cosmosdb.bicep module
// TODO: This can be replaced by using the dataPlaneRoleAssignments parameter in the cosmosDbAccount module directly once the issue is resolved.
// https://github.com/Azure/bicep-registry-modules/issues/5631
var cosmosDbDataPlaneRoleAssignmentsArray = [
  ...(!empty(principalId) ? [
    {
      roleDefinitionIdOrName: 'Cosmos DB Built-in Data Contributor'
      principalType: principalIdType
      principalId: principalId
    }
  ] : [])
]

module cosmosDbDataPlaneRoleAssignments './core/security/role_cosmosdb.bicep' = if (cosmosDbDeploy) {
  name: 'cosmos-db-dataplane-role-assignments-${resourceToken}'
  scope: az.resourceGroup(resourceGroupName)
  dependsOn: [
    rg
    cosmosDbAccount
  ]
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    roleAssignments: cosmosDbDataPlaneRoleAssignmentsArray
  }
}

// --------- STORAGE ACCOUNT ---------
module storageAccount 'br/public:avm/res/storage/storage-account:0.25.0' = if (storageAccountDeploy) {
  name: 'storage-account-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: storageAccountName
    location: location
    kind: 'StorageV2'
    skuName: 'Standard_LRS'
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    blobServices: {
      containers: [
        {
          name: 'genaidbexp'
          publicAccess: 'None'
        }
      ]
    }
    diagnosticSettings: [
      {
        name: 'send-to-log-analytics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- AI SEARCH (OPTIONAL) ---------
module aiSearchService 'br/public:avm/res/search/search-service:0.11.0' = if (azureAiSearchDeploy) {
  name: 'ai-search-service-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: aiSearchName
    location: location
    sku: 'basic'
    diagnosticSettings: [
      {
        name: 'send-to-log-analytics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    disableLocalAuth: false
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    semanticSearch: 'standard'
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Search Service Contributor'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'Search Index Data Contributor'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

output AZURE_RESOURCE_GROUP string = rg.outputs.name
output AZURE_PRINCIPAL_ID string = principalId
output AZURE_PRINCIPAL_ID_TYPE string = principalIdType

// Output the monitoring resources
output LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.outputs.name
output LOG_ANALYTICS_RESOURCE_ID string = logAnalyticsWorkspace.outputs.resourceId
output LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
output APPLICATION_INSIGHTS_NAME string = applicationInsights.outputs.name
output APPLICATION_INSIGHTS_RESOURCE_ID string = applicationInsights.outputs.resourceId
output APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.outputs.instrumentationKey

// Output the AI Search resources
output AZURE_AI_SEARCH_NAME string = aiSearchServiceName
output AZURE_AI_SEARCH_ID   string = aiSearchServiceResourceId

// Output the AI Foundry resources
output AZURE_AI_FOUNDRY_NAME string = aiFoundryService.outputs.name
output AZURE_AI_FOUNDRY_ID string = aiFoundryService.outputs.resourceId
output AZURE_AI_FOUNDRY_ENDPOINT string = aiFoundryService.outputs.endpoint
output AZURE_AI_FOUNDRY_RESOURCE_ID string = aiFoundryService.outputs.resourceId

// Output the SQL Server resources
output SQL_SERVER_NAME string = sqlServer.outputs.name
output SQL_SERVER_RESOURCE_ID string = sqlServer.outputs.resourceId
output SQL_SERVER_ADMIN_USERNAME string = sqlServerUsername
output SQL_DATABASE_ENDPOINT string = sqlServer.outputs.fullyQualifiedDomainName


// Output the Cosmos DB resources
output COSMOS_DB_ACCOUNT_NAME string = cosmosDbAccountNameOut
output COSMOS_DB_ACCOUNT_RESOURCE_ID string = cosmosDbAccountResourceIdOut
output COSMOS_DB_ACCOUNT_ENDPOINT string = cosmosDbAccountEndpointOut

// Output the Storage Account resources
output STORAGE_ACCOUNT_NAME string = storageAccountNameOut
output STORAGE_ACCOUNT_RESOURCE_ID string = storageAccountResourceIdOut
output STORAGE_ACCOUNT_BLOB_ENDPOINT string = storageAccountBlobEndpointOut
